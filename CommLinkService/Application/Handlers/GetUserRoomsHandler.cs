using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Queries;
using CommLinkService.Infrastructure.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class GetUserRoomsHandler : IRequestHandler<GetUserRoomsQuery, GetUserRoomsResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly IWebSocketManager _webSocketManager;
    private readonly ILogger<GetUserRoomsHandler> _logger;

    public GetUserRoomsHandler(
        ICommLinkDbContext context,
        IWebSocketManager webSocketManager,
        ILogger<GetUserRoomsHandler> logger
    )
    {
        _context = context;
        _webSocketManager = webSocketManager;
        _logger = logger;
    }

    public async Task<GetUserRoomsResult> Handle(
        GetUserRoomsQuery request,
        CancellationToken cancellationToken
    )
    {
        var userRooms = await _context
            .RoomParticipants.Include(p => p.Room)
            .ThenInclude(r => r.Participants)
            .Include(p => p.Room)
            .ThenInclude(r => r.Messages)
            .Where(p => p.UserId == request.UserId && p.IsActive && p.Room.IsActive)
            .Select(p => p.Room)
            .ToListAsync(cancellationToken);

        var roomDtos = new List<RoomDto>();

        foreach (var room in userRooms)
        {
            // Contar mensajes no leídos (simplificado - en producción usar tabla de lecturas)
            var unreadCount = room.Messages.Count(m =>
                m.SenderId != request.UserId && m.SentAt > DateTime.UtcNow.AddDays(-7)
            );

            var participants = room
                .Participants.Where(p => p.IsActive)
                .Select(p => new ParticipantDto(
                    p.UserId,
                    "User " + p.UserId.ToString().Substring(0, 8), // Simplificado
                    _webSocketManager.IsUserOnline(p.UserId),
                    p.Role
                ))
                .ToList();

            roomDtos.Add(
                new RoomDto(
                    room.Id,
                    room.Name,
                    room.Type,
                    room.LastActivityAt ?? room.CreatedAt,
                    unreadCount,
                    participants
                )
            );
        }

        return new GetUserRoomsResult(roomDtos.OrderByDescending(r => r.LastActivityAt).ToList());
    }
}
