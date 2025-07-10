using CommLinkServices.Application.DTOs;
using CommLinkServices.Domain;
using CommLinkServices.Infrastructure.Commands;
using CommLinkServices.Infrastructure.Context;
using CommLinkServices.Infrastructure.Hubs;
using Common;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CommEvents;

namespace CommLinkServices.Application.Handlers;

public class StartCallHandler : IRequestHandler<StartCallCommand, ApiResponse<Guid>>
{
    private readonly ILogger<StartCallHandler> _logger;
    private readonly CommLinkDbContext _db;
    private readonly IHubContext<CommHub> _hub;
    private readonly IEventBus _bus;

    public StartCallHandler(
        ILogger<StartCallHandler> logger,
        CommLinkDbContext db,
        IHubContext<CommHub> hub,
        IEventBus bus
    )
    {
        _logger = logger;
        _db = db;
        _hub = hub;
        _bus = bus;
    }

    public async Task<ApiResponse<Guid>> Handle(StartCallCommand request, CancellationToken ct)
    {
        /* 1. Validaciones de negocio --------------------------------------------------------- */
        if (!Enum.TryParse<CallType>(request.Payload.CallType, true, out var parsed))
            return new(false, "CallType must be either 'Voice' or 'Video'");

        // --- LÓGICA DE AUTOCURACIÓN ---
        // Busca si hay una llamada activa en la conversación.
        var inProgressCall = await _db
            .Calls.Where(c => c.ConversationId == request.ConversationId && c.EndedAt == null)
            .FirstOrDefaultAsync(ct);

        // Si existe una llamada "fantasma", la finalizamos.
        if (inProgressCall is not null)
        {
            _logger.LogWarning(
                "Found a stale call {CallId} for conversation {ConvId}. Automatically ending it before starting a new one.",
                inProgressCall.Id,
                request.ConversationId
            );

            inProgressCall.EndedAt = DateTime.UtcNow;
            inProgressCall.UpdatedAt = DateTime.UtcNow;

            // Notificar a los clientes que la llamada fantasma ha terminado, por si acaso.
            await _hub
                .Clients.Group($"convo-{request.ConversationId}")
                .SendAsync(
                    "CallEnded",
                    new
                    {
                        Id = inProgressCall.Id,
                        EndedAt = inProgressCall.EndedAt,
                        Duration = 0,
                    },
                    ct
                );
        }
        // --- FIN DE LA LÓGICA DE AUTOCURACIÓN ---

        /* 2. Persistimos la NUEVA llamada ---------------------------------------------------------- */
        var newCall = new Call
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            StarterId = request.StarterId,
            Type = parsed,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };

        await _db.Calls.AddAsync(newCall, ct);
        await _db.SaveChangesAsync(ct); // Guardamos tanto la finalización de la antigua como la creación de la nueva.

        /* ─── 3. payload SignalR ─────────────────────────────────────────────── */
        var payload = new CallStartedDto(
            newCall.Id,
            newCall.ConversationId,
            newCall.StarterId,
            newCall.Type.ToString(),
            newCall.StartedAt
        );

        /* 4. Evento de dominio → RabbitMQ ---------------------------------------------------- */
        _bus.Publish(
            new CallStartedEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                newCall.ConversationId,
                newCall.Id,
                newCall.StarterId,
                newCall.Type.ToString()
            )
        );

        /* ─── 5. broadcast ───────────────────────────────────────────────────── */
        var convo = await _db
            .Conversations.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == newCall.ConversationId, ct);

        if (convo == null)
        {
            _logger.LogError(
                "FATAL: Conversation {ConvId} not found after creating a call for it.",
                newCall.ConversationId
            );
            return new(false, "Conversation not found, cannot notify participants.");
        }

        var otherUserId = convo.Other(request.StarterId);

        _logger.LogInformation(
            "Broadcasting 'CallStarted' to conversation group 'convo-{ConvId}' and user group 'user-{UserId}'.",
            newCall.ConversationId,
            otherUserId
        );

        await _hub
            .Clients.Group($"convo-{newCall.ConversationId}")
            .SendAsync("CallStarted", payload, ct);
        await _hub.Clients.Group($"user-{otherUserId}").SendAsync("CallStarted", payload, ct);

        _logger.LogInformation(
            "Call {CallId} started by {Starter} in conversation {Conv}",
            newCall.Id,
            newCall.StarterId,
            newCall.ConversationId
        );

        return new(true, "Call started", newCall.Id);
    }
}
