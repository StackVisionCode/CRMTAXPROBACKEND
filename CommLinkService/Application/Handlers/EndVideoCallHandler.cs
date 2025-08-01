using CommLinkService.Domain.Entities;
using CommLinkService.Infrastructure.Commands;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class EndVideoCallHandler : IRequestHandler<EndVideoCallCommand, EndVideoCallResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger<EndVideoCallHandler> _logger;

    public EndVideoCallHandler(
        ICommLinkDbContext context,
        IWebSocketManager webSocketManager,
        ILogger<EndVideoCallHandler> logger
    )
    {
        _context = context;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task<EndVideoCallResult> Handle(
        EndVideoCallCommand request,
        CancellationToken cancellationToken
    )
    {
        var room = await _context
            .Rooms.Include(r => r.Participants)
            .FirstOrDefaultAsync(r => r.Id == request.RoomId, cancellationToken);

        if (room == null)
            return new EndVideoCallResult(false, DateTime.UtcNow);

        // Crear mensaje de sistema para fin de llamada
        var message = new Message(
            request.RoomId,
            request.UserId,
            "Video call ended",
            MessageType.VideoCallEnd,
            System.Text.Json.JsonSerializer.Serialize(new { callId = request.CallId })
        );

        room.AddMessage(message);
        _context.Messages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);

        // Notificar a todos los participantes
        await _webSocketManager.SendToRoomAsync(
            request.RoomId,
            new
            {
                type = "video_call_end",
                data = new
                {
                    callId = request.CallId,
                    roomId = request.RoomId,
                    endedBy = request.UserId,
                },
            }
        );

        _logger.LogInformation(
            "Video call {CallId} ended in room {RoomId}",
            request.CallId,
            request.RoomId
        );

        return new EndVideoCallResult(true, DateTime.UtcNow);
    }
}
