using MediatR;

namespace Infrastructure.Commands;

public record DeleteEmailCommand(Guid Id) : IRequest<Unit>;
