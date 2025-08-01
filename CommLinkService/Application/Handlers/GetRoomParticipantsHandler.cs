using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Queries;
using CommLinkService.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class GetRoomParticipantsHandler
    : IRequestHandler<GetRoomParticipantsQuery, GetRoomParticipantsResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly IWebSocketManager _webSocketManager;

    public GetRoomParticipantsHandler(
        ICommLinkDbContext context,
        IWebSocketManager webSocketManager
    )
    {
        _context = context;
        _webSocketManager = webSocketManager;
    }

    public async Task<GetRoomParticipantsResult> Handle(
        GetRoomParticipantsQuery request,
        CancellationToken cancellationToken
    )
    {
        var participants = await _context
            .RoomParticipants.Where(p => p.RoomId == request.RoomId && p.IsActive)
            .Select(p => new ParticipantDetailDto(
                p.UserId,
                "User " + p.UserId.ToString().Substring(0, 8),
                p.Role,
                _webSocketManager.IsUserOnline(p.UserId),
                p.IsMuted,
                p.IsVideoEnabled,
                p.JoinedAt
            ))
            .ToListAsync(cancellationToken);

        return new GetRoomParticipantsResult(participants);
    }
}
