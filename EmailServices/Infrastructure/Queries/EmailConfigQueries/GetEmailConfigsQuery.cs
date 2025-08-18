using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetEmailConfigsQuery(Guid CompanyId, Guid? TaxUserId = null)
    : IRequest<IEnumerable<EmailConfigDTO>>;
