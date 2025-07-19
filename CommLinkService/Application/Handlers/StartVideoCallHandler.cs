using CommLinkService.Application.Commands;
using CommLinkService.Application.Events;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

namespace CommLinkService.Application.Handlers;

public sealed class StartVideoCallHandler
    : IRequestHandler<StartVideoCallCommand, StartVideoCallResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly IWebSocketManager _webSocketManager;
    private readonly IEventBus _eventBus;
    private readonly ILogger<StartVideoCallHandler> _logger;

    public StartVideoCallHandler(
        ICommLinkDbContext context,
        IWebSocketManager webSocketManager,
        IEventBus eventBus,
        ILogger<StartVideoCallHandler> logger
    )
    {
        _context = context;
        _webSocketManager = webSocketManager;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<StartVideoCallResult> Handle(
        StartVideoCallCommand request,
        CancellationToken cancellationToken
    )
    {
        // 1. Cargar sala + participantes
        var room = await _context
            .Rooms.Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken);

        if (room == null)
            throw new InvalidOperationException("Room not found");

        // 2. Validar iniciador es participante activo
        if (!room.Participants.Any(p => p.UserId == request.InitiatorId && p.IsActive))
            throw new UnauthorizedAccessException("User is not a participant in this room");

        // 3. Normalizar lista de destinatarios
        //    Si request.ParticipantIds viene null o vacía → usar TODOS los activos excepto iniciador.
        var normalizedTargets = (
            request.ParticipantIds != null && request.ParticipantIds.Count > 0
                ? request.ParticipantIds
                : room
                    .Participants.Where(p => p.IsActive && p.UserId != request.InitiatorId)
                    .Select(p => p.UserId)
                    .ToList()
        )!;

        // 4. Crear mensaje de sistema VideoCallStart
        var callId = Guid.NewGuid();
        var metadataJson = System.Text.Json.JsonSerializer.Serialize(new { callId });

        var message = new Message(
            request.RoomId,
            request.InitiatorId,
            "Video call started",
            MessageType.VideoCallStart,
            metadataJson
        );

        room.AddMessage(message);
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        // 5. WS payload → **sólo a otros participantes**
        var wsPayload = new
        {
            type = "video_call_start",
            data = new
            {
                callId,
                roomId = request.RoomId,
                initiatorId = request.InitiatorId,
                participantIds = normalizedTargets,
            },
        };

        foreach (var participantId in normalizedTargets.Where(id => id != request.InitiatorId))
        {
            await _webSocketManager.SendToUserAsync(participantId, wsPayload);
        }

        // 6. EventBus
        _eventBus.Publish(
            new VideoCallStartedEvent(
                Guid.NewGuid(),
                DateTime.UtcNow,
                callId,
                request.RoomId,
                request.InitiatorId,
                normalizedTargets
            )
        );

        _logger.LogInformation(
            "Video call {CallId} started in room {RoomId} by {Initiator} (Targets: {Targets})",
            callId,
            request.RoomId,
            request.InitiatorId,
            string.Join(",", normalizedTargets)
        );

        // Configuración de servidores ICE (STUN/TURN)
        var iceServers = new Dictionary<string, object>
        {
            ["iceServers"] = new[]
            {
                new { urls = "stun:stun.l.google.com:19302" },
                new { urls = "stun:stun1.l.google.com:19302" },
            },
        };

        return new StartVideoCallResult(callId, "ws://localhost:5000/ws/", iceServers);
    }
}
