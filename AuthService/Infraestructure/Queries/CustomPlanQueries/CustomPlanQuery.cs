using AuthService.DTOs.CustomPlanDTOs;
using Common;
using MediatR;

namespace Queries.CustomPlanQueries;

/// <summary>
/// Obtener CustomPlan por ID con módulos incluidos
/// </summary>
public record GetCustomPlanByIdQuery(Guid CustomPlanId) : IRequest<ApiResponse<CustomPlanDTO>>;

/// <summary>
/// Obtener todos los CustomPlans (Solo Developer)
/// Incluye activos e inactivos
/// </summary>
public record GetAllCustomPlansQuery(bool? IsActive = null, bool? IsExpired = null)
    : IRequest<ApiResponse<IEnumerable<CustomPlanDTO>>>;

/// <summary>
/// Obtener CustomPlan por Company ID
/// </summary>
public record GetCustomPlanByCompanyQuery(Guid CompanyId) : IRequest<ApiResponse<CustomPlanDTO>>;

/// <summary>
/// Obtener CustomPlans que expiran pronto
/// </summary>
public record GetExpiringCustomPlansQuery(int DaysAhead = 30)
    : IRequest<ApiResponse<IEnumerable<CustomPlanDTO>>>;

/// <summary>
/// Obtener CustomPlans con estadísticas de uso
/// </summary>
public record GetCustomPlansWithStatsQuery()
    : IRequest<ApiResponse<IEnumerable<CustomPlanWithStatsDTO>>>;

/// <summary>
/// Obtener CustomPlans por rango de precios
/// </summary>
public record GetCustomPlansByPriceRangeQuery(decimal MinPrice, decimal MaxPrice)
    : IRequest<ApiResponse<IEnumerable<CustomPlanDTO>>>;
