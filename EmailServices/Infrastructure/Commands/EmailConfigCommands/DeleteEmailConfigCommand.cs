using MediatR;

namespace Infrastructure.Commands;

public record DeleteEmailConfigCommand(Guid Id) : IRequest<Unit>;

// Unit Representa una operación que se ha realizado con éxito sin necesidad de devolver un valor específico.
