using CommLinkService.Application.Commands;
using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class UpdateParticipantStatusHandler
    : IRequestHandler<UpdateParticipantStatusCommand, UpdateParticipantStatusResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger<UpdateParticipantStatusHandler> _logger;

    public UpdateParticipantStatusHandler(
        ICommLinkDbContext context,
        IWebSocketManager webSocketManager,
        ILogger<UpdateParticipantStatusHandler> logger
    )
    {
        _context = context;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task<UpdateParticipantStatusResult> Handle(
        UpdateParticipantStatusCommand request,
        CancellationToken cancellationToken
    )
    {
        var participant = await _context.RoomParticipants.FirstOrDefaultAsync(
            p => p.RoomId == request.RoomId && p.UserId == request.UserId && p.IsActive,
            cancellationToken
        );

        if (participant == null)
            return new UpdateParticipantStatusResult(false, "Participant not found");

        if (request.IsMuted.HasValue)
            participant.SetMuted(request.IsMuted.Value);

        if (request.IsVideoEnabled.HasValue)
            participant.SetVideoEnabled(request.IsVideoEnabled.Value);

        await _context.SaveChangesAsync(cancellationToken);

        // Notificar a todos en la sala
        await _webSocketManager.SendToRoomAsync(
            request.RoomId,
            new
            {
                type = "participant_status_updated",
                data = new
                {
                    userId = request.UserId,
                    isMuted = participant.IsMuted,
                    isVideoEnabled = participant.IsVideoEnabled,
                },
            },
            request.UserId
        );

        _logger.LogInformation(
            "Participant {UserId} status updated in room {RoomId}",
            request.UserId,
            request.RoomId
        );

        return new UpdateParticipantStatusResult(true, null);
    }
}
