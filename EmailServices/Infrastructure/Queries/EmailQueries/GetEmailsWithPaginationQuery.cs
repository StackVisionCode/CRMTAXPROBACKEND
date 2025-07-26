using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetEmailsWithPaginationQuery(
    int Page = 1,
    int PageSize = 10,
    Guid? UserId = null,
    string? Status = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<PagedResult<EmailDTO>>;
