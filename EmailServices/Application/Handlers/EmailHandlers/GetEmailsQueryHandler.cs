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
    private readonly ILogger<GetEmailsQueryHandler> _log;

    public GetEmailsQueryHandler(EmailContext ctx, IMapper map, ILogger<GetEmailsQueryHandler> log)
    {
        _ctx = ctx;
        _map = map;
        _log = log;
    }

    public async Task<IEnumerable<EmailDTO>> Handle(GetEmailsQuery q, CancellationToken ct)
    {
        // Query eficiente con join para validar configuración activa
        var query =
            from e in _ctx.Emails
            join c in _ctx.EmailConfigs on e.ConfigId equals c.Id
            where
                e.CompanyId == q.CompanyId
                && c.CompanyId == q.CompanyId
                && c.IsActive
                && (
                    !q.TaxUserId.HasValue
                    || e.SentByTaxUserId == q.TaxUserId
                    || e.CreatedByTaxUserId == q.TaxUserId
                )
            orderby e.CreatedOn descending
            select e;

        var list = await query.ToListAsync(ct);

        _log.LogInformation($"Retrieved {list.Count} emails for company {q.CompanyId}");

        return _map.Map<IEnumerable<EmailDTO>>(list);
    }
}
