using CommLinkService.Application.DTOs;
using CommLinkService.Application.Queries;
using CommLinkService.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed class GetRoomBetweenUsersHandler
    : IRequestHandler<GetRoomBetweenUsersQuery, RoomInfoDto?>
{
    private readonly ICommLinkDbContext _db;
    private readonly ILogger<GetRoomBetweenUsersHandler> _log;

    public GetRoomBetweenUsersHandler(
        ICommLinkDbContext db,
        ILogger<GetRoomBetweenUsersHandler> log
    )
    {
        _db = db;
        _log = log;
    }

    public async Task<RoomInfoDto?> Handle(GetRoomBetweenUsersQuery q, CancellationToken ct)
    {
        try
        {
            // Si alguien pasa los mismos usuarios (raro), protegemos.
            if (q.User1Id == q.User2Id)
            {
                _log.LogWarning(
                    "GetRoomBetweenUsers: mismos IDs ({User}). Se devolverá la sala más reciente donde esté el usuario.",
                    q.User1Id
                );
            }

            // JOIN doble: r -> p1 (user1) -> p2 (user2)
            // Filtramos IsActive tanto de Room como (opcional) de participantes
            var query =
                from r in _db.Rooms.AsNoTracking()
                join p1 in _db.RoomParticipants.AsNoTracking() on r.Id equals p1.RoomId
                join p2 in _db.RoomParticipants.AsNoTracking() on r.Id equals p2.RoomId
                where
                    r.IsActive
                    && p1.UserId == q.User1Id
                    && p2.UserId == q.User2Id
                    && p1.IsActive // <-- quita si quieres incluir inactivos
                    && p2.IsActive // <--
                select r;

            if (q.Type is not null)
            {
                query = query.Where(r => r.Type == q.Type);
            }

            // Traemos la sala más reciente (LastActivityAt si no nulo; sino CreatedAt)
            var dto = await query
                .OrderByDescending(r => r.LastActivityAt ?? r.CreatedAt)
                .Select(r => new RoomInfoDto(
                    r.Id,
                    r.Name,
                    r.Type,
                    r.CreatedAt,
                    r.CreatedBy,
                    r.LastActivityAt ?? r.CreatedAt
                ))
                .FirstOrDefaultAsync(ct);

            _log.LogDebug(
                "GetRoomBetweenUsersHandler({U1},{U2},{Type}) => {Result}",
                q.User1Id,
                q.User2Id,
                q.Type?.ToString() ?? "<any>",
                dto?.RoomId.ToString() ?? "NONE"
            );

            return dto;
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error buscando sala ({Type}) entre {U1} y {U2}",
                q.Type?.ToString() ?? "<any>",
                q.User1Id,
                q.User2Id
            );

            return null;
        }
    }
}
