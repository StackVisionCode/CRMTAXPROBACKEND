using Application.Common.DTO;
using AutoMapper;
using EmailServices.Domain;
using Infrastructure.Context;
using Infrastructure.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers;

public class GetEmailsWithPaginationHandler
    : IRequestHandler<GetEmailsWithPaginationQuery, PagedResult<EmailDTO>>
{
    private readonly EmailContext _ctx;
    private readonly IMapper _map;
    private readonly ILogger<GetEmailsWithPaginationHandler> _log;

    public GetEmailsWithPaginationHandler(
        EmailContext ctx,
        IMapper map,
        ILogger<GetEmailsWithPaginationHandler> log
    )
    {
        _ctx = ctx;
        _map = map;
        _log = log;
    }

    public async Task<PagedResult<EmailDTO>> Handle(
        GetEmailsWithPaginationQuery query,
        CancellationToken ct
    )
    {
        var dbQuery = _ctx.Emails.Where(e => e.CompanyId == query.CompanyId);

        // Filtros adicionales
        if (query.TaxUserId.HasValue)
            dbQuery = dbQuery.Where(e =>
                e.SentByTaxUserId == query.TaxUserId || e.CreatedByTaxUserId == query.TaxUserId
            );

        if (
            !string.IsNullOrEmpty(query.Status)
            && Enum.TryParse<EmailStatus>(query.Status, out var status)
        )
            dbQuery = dbQuery.Where(e => e.Status == status);

        if (query.FromDate.HasValue)
            dbQuery = dbQuery.Where(e => e.CreatedOn >= query.FromDate);

        if (query.ToDate.HasValue)
            dbQuery = dbQuery.Where(e => e.CreatedOn <= query.ToDate);

        // Contar total
        var totalItems = await dbQuery.CountAsync(ct);
        var totalPages = (int)Math.Ceiling((double)totalItems / query.PageSize);

        // Paginación
        var emails = await dbQuery
            .OrderByDescending(e => e.CreatedOn)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        _log.LogInformation(
            $"Retrieved page {query.Page} with {emails.Count} emails for company {query.CompanyId}"
        );

        return new PagedResult<EmailDTO>
        {
            Items = _map.Map<IEnumerable<EmailDTO>>(emails),
            TotalItems = totalItems,
            TotalPages = totalPages,
            CurrentPage = query.Page,
            PageSize = query.PageSize,
        };
    }
}
