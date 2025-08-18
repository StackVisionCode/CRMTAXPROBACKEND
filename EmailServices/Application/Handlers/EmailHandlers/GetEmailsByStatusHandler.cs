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
    private readonly ILogger<GetEmailsByStatusHandler> _log;

    public GetEmailsByStatusHandler(
        EmailContext ctx,
        IMapper map,
        ILogger<GetEmailsByStatusHandler> log
    )
    {
        _ctx = ctx;
        _map = map;
        _log = log;
    }

    public async Task<IEnumerable<EmailDTO>> Handle(
        GetEmailsByStatusQuery query,
        CancellationToken ct
    )
    {
        var dbQuery = _ctx.Emails.Where(e =>
            e.Status == query.Status && e.CompanyId == query.CompanyId
        );

        // Filtrar por TaxUserId si se proporciona
        if (query.TaxUserId.HasValue)
            dbQuery = dbQuery.Where(e =>
                e.SentByTaxUserId == query.TaxUserId || e.CreatedByTaxUserId == query.TaxUserId
            );

        var emails = await dbQuery.OrderByDescending(e => e.CreatedOn).ToListAsync(ct);

        _log.LogInformation(
            $"Retrieved {emails.Count} emails with status {query.Status} for company {query.CompanyId}"
        );

        return _map.Map<IEnumerable<EmailDTO>>(emails);
    }
}
