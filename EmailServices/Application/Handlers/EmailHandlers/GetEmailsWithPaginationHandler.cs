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

    public GetEmailsWithPaginationHandler(EmailContext ctx, IMapper map)
    {
        _ctx = ctx;
        _map = map;
    }

    public async Task<PagedResult<EmailDTO>> Handle(
        GetEmailsWithPaginationQuery query,
        CancellationToken ct
    )
    {
        var dbQuery = _ctx.Emails.AsQueryable();

        // Filtros
        if (query.UserId.HasValue)
            dbQuery = dbQuery.Where(e => e.SentByUserId == query.UserId);

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
