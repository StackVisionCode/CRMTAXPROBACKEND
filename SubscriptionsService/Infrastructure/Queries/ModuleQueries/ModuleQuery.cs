using Common;
using DTOs.ModuleDTOs;
using MediatR;

namespace Queries.ModuleQueries;

/// <summary>
/// Obtener Module por ID con información del Service
/// </summary>
public record GetModuleByIdQuery(Guid ModuleId) : IRequest<ApiResponse<ModuleDTO>>;

/// <summary>
/// Obtener todos los Modules (Solo Developer)
/// Incluye activos e inactivos
/// </summary>
public record GetAllModulesQuery(bool? IsActive = null, Guid? ServiceId = null)
    : IRequest<ApiResponse<IEnumerable<ModuleDTO>>>;

/// <summary>
/// Obtener Modules activos para selección
/// </summary>
public record GetActiveModulesQuery(Guid? ServiceId = null)
    : IRequest<ApiResponse<IEnumerable<ModuleDTO>>>;

/// <summary>
/// Obtener Modules disponibles para agregar a CustomPlan
/// (Modules que no están en un Service base o están como adicionales)
/// </summary>
public record GetAvailableModulesForCustomPlanQuery()
    : IRequest<ApiResponse<IEnumerable<ModuleDTO>>>;

/// <summary>
/// Obtener Modules por Service ID
/// </summary>
public record GetModulesByServiceQuery(Guid ServiceId)
    : IRequest<ApiResponse<IEnumerable<ModuleDTO>>>;

/// <summary>
/// Obtener Modules con estadísticas de uso
/// </summary>
public record GetModulesWithStatsQuery() : IRequest<ApiResponse<IEnumerable<ModuleWithStatsDTO>>>;

/// <summary>
/// DTO para estadísticas de Module
/// </summary>
public class ModuleWithStatsDTO : ModuleDTO
{
    public int CustomPlansUsingCount { get; set; }
    public int CompaniesUsingCount { get; set; }
    public bool IsBaseModule { get; set; } // Si pertenece a un Service
    public bool IsAdditionalModule { get; set; } // Si se usa como adicional
}
