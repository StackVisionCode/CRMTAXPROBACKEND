using AuthService.DTOs.ServiceDTOs;
using Common;
using MediatR;

namespace Commands.ServiceCommands;

/// <summary>
/// Activar/Desactivar un Service (Solo Developer)
/// </summary>
public record ToggleServiceStatusCommand(Guid ServiceId, bool IsActive)
    : IRequest<ApiResponse<ServiceDTO>>;
