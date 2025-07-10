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

        var inProgress = await _db
            .Calls.Where(c => c.ConversationId == request.ConversationId && c.EndedAt == null)
            .FirstOrDefaultAsync(ct);

        if (inProgress is not null)
            return new(false, "Ya existe una llamada activa.", inProgress.Id);

        /* 2. Persistimos la llamada ---------------------------------------------------------- */
        var call = new Call
        {
            Id = Guid.NewGuid(),
            ConversationId = request.ConversationId,
            StarterId = request.StarterId,
            Type = parsed,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };

        await _db.Calls.AddAsync(call, ct);
        await _db.SaveChangesAsync(ct);

        /* ─── 3. payload SignalR ─────────────────────────────────────────────── */
        var payload = new CallStartedDto(
            call.Id,
            call.ConversationId,
            call.StarterId,
            call.Type.ToString(),
            call.StartedAt
        );

        /* 4. Evento de dominio → RabbitMQ (opcional, lo mantenemos) -------------------------- */
        _bus.Publish(
            new CallStartedEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                call.ConversationId,
                call.Id,
                call.StarterId,
                call.Type.ToString()
            )
        );

        /* ─── 5. broadcast ───────────────────────────────────────────────────── */
        // A) grupo de la conversación (si alguien ya está dentro)
        await _hub
            .Clients.Group($"convo-{call.ConversationId}")
            .SendAsync("CallStarted", payload, ct);

        // B) grupos personales de los demás usuarios
        var convo = await _db
            .Conversations.AsNoTracking()
            .FirstAsync(c => c.Id == call.ConversationId, ct);

        foreach (
            var uid in new[] { convo.FirstUserId, convo.SecondUserId }.Where(u =>
                u != call.StarterId
            )
        )
        {
            await _hub.Clients.Group($"user-{uid}").SendAsync("CallStarted", payload, ct);
        }

        _logger.LogInformation(
            "Call {CallId} started by {Starter} in conversation {Conv}",
            call.Id,
            call.StarterId,
            call.ConversationId
        );

        return new(true, "Call started", call.Id);
    }
}
