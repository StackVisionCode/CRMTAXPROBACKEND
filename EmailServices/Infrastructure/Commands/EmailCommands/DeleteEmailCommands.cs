using MediatR;

namespace Infrastructure.Commands;

public record DeleteEmailCommand(Guid Id, Guid CompanyId, Guid DeletedByTaxUserId) : IRequest<Unit>;
