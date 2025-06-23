using AutoMapper;
using AutoMapper.QueryableExtensions;
using CommLinkServices.Application.DTOs;
using CommLinkServices.Infrastructure.Context;
using CommLinkServices.Infrastructure.Queries;
using Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkServices.Application.Handlers;

public class GetMessagesHandler
    : IRequestHandler<GetMessagesQuery, ApiResponse<IEnumerable<MessageDto>>>
{
    private readonly ILogger<GetMessagesHandler> _logger;
    private readonly CommLinkDbContext _db;
    private readonly IMapper _mapper;

    public GetMessagesHandler(
        ILogger<GetMessagesHandler> logger,
        CommLinkDbContext db,
        IMapper mapper
    )
    {
        _logger = logger;
        _db = db;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<MessageDto>>> Handle(
        GetMessagesQuery request,
        CancellationToken cancellationToken
    )
    {
        var belongs = await _db.Conversations.AnyAsync(
            c =>
                c.Id == request.ConversationId
                && (c.FirstUserId == request.UserId || c.SecondUserId == request.UserId),
            cancellationToken
        );
        if (!belongs)
            return new(false, "Forbidden");

        var query = _db.Messages.Where(m => m.ConversationId == request.ConversationId);

        if (request.After is not null)
            query = query.Where(m => m.SentAt > request.After);

        var list = await query
            .OrderBy(m => m.SentAt)
            .AsNoTracking()
            .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return new(true, "Ok", list);
    }
}
