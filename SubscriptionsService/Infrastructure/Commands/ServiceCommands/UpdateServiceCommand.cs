using Common;
using DTOs.ServiceDTOs;
using MediatR;

namespace Commands.ServiceCommands;

/// <summary>
/// Actualizar un Service existente (Solo Developer)
/// </summary>
public record UpdateServiceCommand(UpdateServiceDTO ServiceData)
    : IRequest<ApiResponse<ServiceDTO>>;
