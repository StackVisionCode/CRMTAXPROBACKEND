using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetIncomingEmailsQuery(Guid CompanyId, Guid? TaxUserId = null, bool? IsRead = null)
    : IRequest<IEnumerable<IncomingEmailDTO>>;
