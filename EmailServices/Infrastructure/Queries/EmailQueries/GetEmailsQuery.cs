using Application.Common.DTO;
using MediatR;

namespace Infrastructure.Queries;

public record GetEmailsQuery(Guid CompanyId, Guid? TaxUserId = null)
    : IRequest<IEnumerable<EmailDTO>>;
