using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetEmailConfigsQuery(Guid? UserId) : IRequest<IEnumerable<EmailConfigDTO>>;
