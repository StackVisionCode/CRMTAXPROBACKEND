using MediatR;

namespace Infrastructure.Commands;

public record MarkIncomingEmailAsReadCommand(Guid Id) : IRequest<Unit>;
