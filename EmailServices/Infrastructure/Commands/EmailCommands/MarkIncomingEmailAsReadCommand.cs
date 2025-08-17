using MediatR;

namespace Infrastructure.Commands;

public record MarkIncomingEmailAsReadCommand(Guid Id, Guid CompanyId, Guid ModifiedByTaxUserId)
    : IRequest<Unit>;
