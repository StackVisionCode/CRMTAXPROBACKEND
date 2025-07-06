using Application.Common.DTO;
using AutoMapper;
using Infrastructure.Context;
using Infrastructure.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetEmailsQueryHandler : IRequestHandler<GetEmailsQuery, IEnumerable<EmailDTO>>
{
    private readonly EmailContext _ctx;
    private readonly IMapper _map;

    public GetEmailsQueryHandler(EmailContext ctx, IMapper map)
    {
        _ctx = ctx;
        _map = map;
    }

    // TaxUser y CompanyServices son servicios independientes

    // public async Task<IEnumerable<EmailDTO>> Handle(GetEmailsQuery q, CancellationToken ct)
    // {
    //     var query = _ctx.Emails.AsQueryable();

    //     if (q.UserId.HasValue)
    //         query = query.Where(e => e.SentByUserId == q.UserId);

    //     var list = await query.OrderByDescending(e => e.CreatedOn)
    //                         .ToListAsync(ct);

    //     return _map.Map<IEnumerable<EmailDTO>>(list);
    // }

    public async Task<IEnumerable<EmailDTO>> Handle(GetEmailsQuery q, CancellationToken ct)
    {
        // join Emails ↔ EmailConfigs
        var query =
            from e in _ctx.Emails
            join c in _ctx.EmailConfigs on e.ConfigId equals c.Id
            where !q.UserId.HasValue || e.SentByUserId == q.UserId
            orderby e.CreatedOn descending
            select e;

        var list = await query.ToListAsync(ct);
        return _map.Map<IEnumerable<EmailDTO>>(list);
    }
}
