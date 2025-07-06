using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetEmailsQuery(Guid? UserId) : IRequest<IEnumerable<EmailDTO>>;
