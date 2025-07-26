using Application.Common.DTO;
using AutoMapper;
using Infrastructure.Context;
using Infrastructure.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetEmailsByStatusHandler
    : IRequestHandler<GetEmailsByStatusQuery, IEnumerable<EmailDTO>>
{
    private readonly EmailContext _ctx;
    private readonly IMapper _map;

    public GetEmailsByStatusHandler(EmailContext ctx, IMapper map)
    {
        _ctx = ctx;
        _map = map;
    }

    public async Task<IEnumerable<EmailDTO>> Handle(
        GetEmailsByStatusQuery query,
        CancellationToken ct
    )
    {
        var dbQuery = _ctx.Emails.Where(e => e.Status == query.Status);

        if (query.UserId.HasValue)
            dbQuery = dbQuery.Where(e => e.SentByUserId == query.UserId);

        var emails = await dbQuery.OrderByDescending(e => e.CreatedOn).ToListAsync(ct);

        return _map.Map<IEnumerable<EmailDTO>>(emails);
    }
}
