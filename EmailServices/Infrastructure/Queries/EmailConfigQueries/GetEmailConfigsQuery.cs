using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetEmailConfigsQuery(int? CompanyId, int? UserId) : IRequest<IEnumerable<EmailConfigDTO>>;