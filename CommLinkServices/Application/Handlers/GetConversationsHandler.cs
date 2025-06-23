using AutoMapper;
using CommLinkServices.Application.DTOs;
using CommLinkServices.Infrastructure.Context;
using CommLinkServices.Infrastructure.Queries;
using Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkServices.Application.Handlers;

public class GetConversationsHandler
    : IRequestHandler<GetConversationsQuery, ApiResponse<IEnumerable<ConversationDto>>>
{
    private readonly ILogger<GetConversationsHandler> _logger;
    private readonly CommLinkDbContext _db;
    private readonly IMapper _mapper;

    public GetConversationsHandler(
        ILogger<GetConversationsHandler> logger,
        CommLinkDbContext db,
        IMapper mapper
    )
    {
        _logger = logger;
        _db = db;
        _mapper = mapper;
    }

    public async Task<ApiResponse<IEnumerable<ConversationDto>>> Handle(
        GetConversationsQuery request,
        CancellationToken cancellationToken
    )
    {
        var baseQuery = _db
            .Conversations.Where(c =>
                c.FirstUserId == request.UserId || c.SecondUserId == request.UserId
            )
            .Select(c => new
            {
                c,
                LastMessage = _db
                    .Messages.Where(m => m.ConversationId == c.Id)
                    .OrderByDescending(m => m.SentAt)
                    .Select(m => new { m.Content, m.SentAt })
                    .FirstOrDefault(),
            });

        var list = await baseQuery.AsNoTracking().ToListAsync(cancellationToken);

        var mapped = list.Select(x =>
            {
                var dto = _mapper.Map<ConversationDto>(
                    x.c,
                    opt => opt.Items["Me"] = request.UserId
                );

                // Instead of using 'with', directly set the properties
                dto.LastActivity = x.LastMessage?.SentAt ?? x.c.CreatedAt;
                dto.LastSnippet = x.LastMessage?.Content;

                return dto;
            })
            .OrderByDescending(d => d.LastActivity);

        return new(true, "Ok", mapped);
    }
}
