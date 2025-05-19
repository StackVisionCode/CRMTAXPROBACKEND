using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetEmailsQuery(int? CompanyId, int? UserId) : IRequest<IEnumerable<EmailDTO>>;