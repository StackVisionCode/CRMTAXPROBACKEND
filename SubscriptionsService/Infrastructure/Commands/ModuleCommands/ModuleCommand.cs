using Common;
using DTOs.ModuleDTOs;
using MediatR;

namespace Commands.ModuleCommands;

/// <summary>
/// Crear un nuevo Module (Solo Developer)
/// </summary>
public record CreateModuleCommand(NewModuleDTO ModuleData) : IRequest<ApiResponse<ModuleDTO>>;

/// <summary>
/// Actualizar un Module existente (Solo Developer)
/// </summary>
public record UpdateModuleCommand(UpdateModuleDTO ModuleData) : IRequest<ApiResponse<ModuleDTO>>;

/// <summary>
/// Eliminar un Module (Solo Developer)
/// Soft delete si está siendo usado por CustomModules
/// </summary>
public record DeleteModuleCommand(Guid ModuleId) : IRequest<ApiResponse<bool>>;

/// <summary>
/// Activar/Desactivar un Module (Solo Developer)
/// </summary>
public record ToggleModuleStatusCommand(Guid ModuleId, bool IsActive)
    : IRequest<ApiResponse<ModuleDTO>>;

/// <summary>
/// Asignar Module a Service (Solo Developer)
/// </summary>
public record AssignModuleToServiceCommand(Guid ModuleId, Guid? ServiceId)
    : IRequest<ApiResponse<ModuleDTO>>;
