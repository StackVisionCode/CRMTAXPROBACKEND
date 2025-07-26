using Application.Common.DTO;
using AutoMapper;
using Infrastructure.Context;
using Infrastructure.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetIncomingEmailsHandler
    : IRequestHandler<GetIncomingEmailsQuery, IEnumerable<IncomingEmailDTO>>
{
    private readonly EmailContext _ctx;
    private readonly IMapper _map;

    public GetIncomingEmailsHandler(EmailContext ctx, IMapper map)
    {
        _ctx = ctx;
        _map = map;
    }

    public async Task<IEnumerable<IncomingEmailDTO>> Handle(
        GetIncomingEmailsQuery query,
        CancellationToken ct
    )
    {
        var dbQuery = _ctx.IncomingEmails.Include(e => e.Attachments).AsQueryable();

        if (query.UserId.HasValue)
            dbQuery = dbQuery.Where(e => e.UserId == query.UserId);

        if (query.IsRead.HasValue)
            dbQuery = dbQuery.Where(e => e.IsRead == query.IsRead);

        var emails = await dbQuery.OrderByDescending(e => e.ReceivedOn).ToListAsync(ct);

        return _map.Map<IEnumerable<IncomingEmailDTO>>(emails);
    }
}
