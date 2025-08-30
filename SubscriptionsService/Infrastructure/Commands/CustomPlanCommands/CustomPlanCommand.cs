using Common;
using DTOs.CustomPlanDTOs;
using MediatR;

namespace Commands.CustomPlanCommands;

/// <summary>
/// Crear un nuevo CustomPlan (Solo Developer)
/// </summary>
public record CreateCustomPlanCommand(NewCustomPlanDTO CustomPlanData)
    : IRequest<ApiResponse<CustomPlanDTO>>;

/// <summary>
/// Actualizar un CustomPlan existente (Solo Developer)
/// </summary>
public record UpdateCustomPlanCommand(UpdateCustomPlanDTO CustomPlanData)
    : IRequest<ApiResponse<CustomPlanDTO>>;

/// <summary>
/// Eliminar un CustomPlan (Solo Developer)
/// No se puede eliminar si la Company está activa
/// </summary>
public record DeleteCustomPlanCommand(Guid CustomPlanId) : IRequest<ApiResponse<bool>>;

/// <summary>
/// Activar/Desactivar un CustomPlan (Solo Developer)
/// </summary>
public record ToggleCustomPlanStatusCommand(Guid CustomPlanId, bool IsActive)
    : IRequest<ApiResponse<CustomPlanDTO>>;

/// <summary>
/// Renovar un CustomPlan (Solo Developer)
/// Actualiza fechas y estado de renovación
/// </summary>
public record RenewCustomPlanCommand(Guid CustomPlanId, DateTime? NewEndDate = null)
    : IRequest<ApiResponse<CustomPlanDTO>>;

/// <summary>
/// Actualizar precio de CustomPlan (Solo Developer)
/// </summary>
public record UpdateCustomPlanPriceCommand(Guid CustomPlanId, decimal NewPrice)
    : IRequest<ApiResponse<CustomPlanDTO>>;
