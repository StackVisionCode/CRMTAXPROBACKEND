using AuthService.DTOs.CustomModuleDTOs;
using Common;
using MediatR;

namespace Queries.CustomModuleQueries;

/// <summary>
/// Obtener CustomModule por ID con información del Module
/// </summary>
public record GetCustomModuleByIdQuery(Guid CustomModuleId)
    : IRequest<ApiResponse<CustomModuleDTO>>;

/// <summary>
/// Obtener todos los CustomModules (Solo Developer)
/// Incluye incluidos y excluidos
/// </summary>
public record GetAllCustomModulesQuery(bool? IsIncluded = null, Guid? CustomPlanId = null)
    : IRequest<ApiResponse<IEnumerable<CustomModuleDTO>>>;

/// <summary>
/// Obtener CustomModules por CustomPlan ID
/// </summary>
public record GetCustomModulesByPlanQuery(Guid CustomPlanId, bool? IsIncluded = null)
    : IRequest<ApiResponse<IEnumerable<CustomModuleDTO>>>;

/// <summary>
/// Obtener CustomModules por Module ID
/// Útil para ver qué CustomPlans usan un módulo específico
/// </summary>
public record GetCustomModulesByModuleQuery(Guid ModuleId, bool? IsIncluded = null)
    : IRequest<ApiResponse<IEnumerable<CustomModuleDTO>>>;

/// <summary>
/// Obtener CustomModules incluidos por Company ID
/// Para saber qué módulos tiene disponibles una Company
/// </summary>
public record GetActiveCustomModulesByCompanyQuery(Guid CompanyId)
    : IRequest<ApiResponse<IEnumerable<CustomModuleDTO>>>;

/// <summary>
/// Obtener CustomModules con estadísticas de uso
/// </summary>
public record GetCustomModulesWithStatsQuery()
    : IRequest<ApiResponse<IEnumerable<CustomModuleWithStatsDTO>>>;

/// <summary>
/// Verificar disponibilidad de módulos para un CustomPlan
/// Devuelve módulos que pueden ser agregados al plan
/// </summary>
public record GetAvailableModulesForCustomPlanQuery(Guid CustomPlanId)
    : IRequest<ApiResponse<IEnumerable<ModuleAvailabilityDTO>>>;

/// <summary>
/// DTO para estadísticas de CustomModule
/// </summary>
public class CustomModuleWithStatsDTO : CustomModuleDTO
{
    public string? CompanyName { get; set; }
    public string? CompanyDomain { get; set; }
    public bool CustomPlanIsActive { get; set; }
    public int CustomPlanUserLimit { get; set; }
    public decimal CustomPlanPrice { get; set; }
    public int DaysUntilPlanExpiry { get; set; }
}

/// <summary>
/// DTO para disponibilidad de módulos
/// </summary>
public class ModuleAvailabilityDTO
{
    public Guid ModuleId { get; set; }
    public string ModuleName { get; set; } = string.Empty;
    public string ModuleDescription { get; set; } = string.Empty;
    public string? ModuleUrl { get; set; }
    public bool IsAvailable { get; set; }
    public string? UnavailableReason { get; set; }
    public bool IsAlreadyIncluded { get; set; }
    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }
}
