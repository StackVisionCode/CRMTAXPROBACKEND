using Common;
using MediatR;

namespace Commands.ServiceCommands;

/// <summary>
/// Eliminar un Service (Solo Developer)
/// Soft delete si tiene Companies asociadas
/// </summary>
public record DeleteServiceCommand(Guid ServiceId) : IRequest<ApiResponse<bool>>;
