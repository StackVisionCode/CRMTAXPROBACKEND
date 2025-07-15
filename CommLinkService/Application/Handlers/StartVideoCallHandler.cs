using CommLinkService.Application.Commands;
using CommLinkService.Application.Events;
using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

namespace CommLinkService.Application.Handlers;

public sealed class StartVideoCallHandler : IRequestHandler<StartVideoCallCommand, StartVideoCallResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly IWebSocketManager _webSocketManager;
    private readonly IEventBus _eventBus;
    private readonly ILogger<StartVideoCallHandler> _logger;

    public StartVideoCallHandler(
        ICommLinkDbContext context,
        IWebSocketManager webSocketManager,
        IEventBus eventBus,
        ILogger<StartVideoCallHandler> logger)
    {
        _context = context;
        _webSocketManager = webSocketManager;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<StartVideoCallResult> Handle(
        StartVideoCallCommand request,
        CancellationToken cancellationToken)
    {
        var room = await _context.Rooms
            .Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken);

        if (room == null)
            throw new InvalidOperationException("Room not found");

        // Verificar que el iniciador es participante
        if (!room.Participants.Any(p => p.UserId == request.InitiatorId && p.IsActive))
            throw new UnauthorizedAccessException("User is not a participant in this room");

        // Crear mensaje de sistema para inicio de llamada
        var callId = Guid.NewGuid();
        var message = new Message(
            request.RoomId,
            request.InitiatorId,
            "Video call started",
            MessageType.VideoCallStart,
            System.Text.Json.JsonSerializer.Serialize(new { callId })
        );

        room.AddMessage(message);
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        // IMPORTANTE: Solo notificar a los participantes que NO son el iniciador
        var callData = new
        {
            type = "video_call_start",
            data = new
            {
                callId,
                roomId = request.RoomId,
                initiatorId = request.InitiatorId,
                participantIds = request.ParticipantIds
            }
        };

        // Enviar notificación solo a otros participantes (excluyendo al iniciador)
        foreach (var participantId in request.ParticipantIds.Where(id => id != request.InitiatorId))
        {
            await _webSocketManager.SendToUserAsync(participantId, callData);
        }

        // Publicar evento
        _eventBus.Publish(new VideoCallStartedEvent(
            Guid.NewGuid(),
            DateTime.UtcNow,
            callId,
            request.RoomId,
            request.InitiatorId,
            request.ParticipantIds
        ));

        _logger.LogInformation("Video call {CallId} started in room {RoomId}", callId, request.RoomId);

        // Configuración de servidores ICE (STUN/TURN)
        var iceServers = new Dictionary<string, object>
        {
            ["iceServers"] = new[]
            {
                new { urls = "stun:stun.l.google.com:19302" },
                new { urls = "stun:stun1.l.google.com:19302" }
            }
        };

        return new StartVideoCallResult(callId, "wss://localhost:5000/ws/commlink", iceServers);
    }
}