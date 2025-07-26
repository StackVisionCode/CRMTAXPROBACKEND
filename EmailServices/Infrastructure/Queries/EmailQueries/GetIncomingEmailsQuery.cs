using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetIncomingEmailsQuery(Guid? UserId = null, bool? IsRead = null)
    : IRequest<IEnumerable<IncomingEmailDTO>>;
