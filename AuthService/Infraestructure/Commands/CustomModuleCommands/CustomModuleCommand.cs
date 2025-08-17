using AuthService.DTOs.CustomModuleDTOs;
using Common;
using MediatR;

namespace Commands.CustomModuleCommands;

/// <summary>
/// Asignar un Module a un CustomPlan (Solo Developer)
/// </summary>
public record AssignCustomModuleCommand(AssignCustomModuleDTO CustomModuleData)
    : IRequest<ApiResponse<CustomModuleDTO>>;

/// <summary>
/// Actualizar un CustomModule existente (Solo Developer)
/// </summary>
public record UpdateCustomModuleCommand(UpdateCustomModuleDTO CustomModuleData)
    : IRequest<ApiResponse<CustomModuleDTO>>;

/// <summary>
/// Eliminar un CustomModule (Solo Developer)
/// Remueve el módulo del CustomPlan
/// </summary>
public record RemoveCustomModuleCommand(Guid CustomModuleId) : IRequest<ApiResponse<bool>>;

/// <summary>
/// Activar/Desactivar un CustomModule (Solo Developer)
/// Cambia el estado IsIncluded
/// </summary>
public record ToggleCustomModuleCommand(Guid CustomModuleId, bool IsIncluded)
    : IRequest<ApiResponse<CustomModuleDTO>>;

/// <summary>
/// Asignar múltiples módulos a un CustomPlan (Solo Developer)
/// </summary>
public record BulkAssignCustomModulesCommand(Guid CustomPlanId, ICollection<Guid> ModuleIds)
    : IRequest<ApiResponse<IEnumerable<CustomModuleDTO>>>;

/// <summary>
/// Remover múltiples módulos de un CustomPlan (Solo Developer)
/// </summary>
public record BulkRemoveCustomModulesCommand(Guid CustomPlanId, ICollection<Guid> ModuleIds)
    : IRequest<ApiResponse<bool>>;
