using CommLinkService.Application.Commands;
using CommLinkService.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class LeaveRoomHandler : IRequestHandler<LeaveRoomCommand, LeaveRoomResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly ILogger<LeaveRoomHandler> _logger;

    public LeaveRoomHandler(ICommLinkDbContext context, ILogger<LeaveRoomHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<LeaveRoomResult> Handle(
        LeaveRoomCommand request,
        CancellationToken cancellationToken
    )
    {
        var participant = await _context.RoomParticipants.FirstOrDefaultAsync(
            p => p.RoomId == request.RoomId && p.UserId == request.UserId && p.IsActive,
            cancellationToken
        );

        if (participant == null)
            return new LeaveRoomResult(false);

        participant.SetInactive();
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} left room {RoomId}", request.UserId, request.RoomId);

        return new LeaveRoomResult(true);
    }
}
