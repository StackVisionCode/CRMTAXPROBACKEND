using CommLinkService.Infrastructure.Persistence;
using CommLinkService.Infrastructure.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class GetRoomMessagesHandler
    : IRequestHandler<GetRoomMessagesQuery, GetRoomMessagesResult>
{
    private readonly ICommLinkDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GetRoomMessagesHandler> _logger;

    public GetRoomMessagesHandler(
        ICommLinkDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<GetRoomMessagesHandler> logger
    )
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<GetRoomMessagesResult> Handle(
        GetRoomMessagesQuery request,
        CancellationToken cancellationToken
    )
    {
        var totalCount = await _context.Messages.CountAsync(
            m => m.RoomId == request.RoomId && !m.IsDeleted,
            cancellationToken
        );

        var messages = await _context
            .Messages.Include(m => m.Reactions)
            .Where(m => m.RoomId == request.RoomId && !m.IsDeleted)
            .OrderByDescending(m => m.SentAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Obtener información de usuarios desde el servicio Auth
        var userIds = messages.Select(m => m.SenderId).Distinct().ToList();
        var userNames = await GetUserNamesAsync(userIds);

        var messageDtos = messages
            .Select(m => new MessageDto(
                m.Id,
                m.SenderId,
                userNames.GetValueOrDefault(m.SenderId, "Unknown User"),
                m.Content,
                m.Type,
                m.SentAt,
                m.EditedAt.HasValue,
                m.Reactions.GroupBy(r => r.Emoji)
                    .Select(g => new ReactionDto(g.Key, g.Select(r => r.UserId).ToList()))
                    .ToList()
            ))
            .ToList();

        var hasMore = totalCount > request.PageNumber * request.PageSize;

        return new GetRoomMessagesResult(messageDtos, totalCount, hasMore);
    }

    private async Task<Dictionary<Guid, string>> GetUserNamesAsync(List<Guid> userIds)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("Auth");
            // Implementar llamada al servicio Auth para obtener nombres
            // Por ahora retornamos un diccionario vacío
            return new Dictionary<Guid, string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user names from Auth service");
            return new Dictionary<Guid, string>();
        }
    }
}
