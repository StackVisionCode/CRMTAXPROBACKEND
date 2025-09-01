using Common;
using DTOs.ServiceDTOs;
using MediatR;

namespace Commands.ServiceCommands;

/// <summary>
/// Crear un nuevo Service (Solo Developer)
/// </summary>
public record CreateServiceCommand(NewServiceDTO ServiceData) : IRequest<ApiResponse<ServiceDTO>>;
