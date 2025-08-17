using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetEmailsWithPaginationQuery(
    Guid CompanyId,
    int Page = 1,
    int PageSize = 10,
    Guid? TaxUserId = null,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<PagedResult<EmailDTO>>;
