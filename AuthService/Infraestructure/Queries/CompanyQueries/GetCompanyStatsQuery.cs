using AuthService.DTOs.CompanyDTOs;
using Common;
using MediatR;

namespace Queries.CompanyQueries;

/// <summary>
/// Obtiene estadísticas completas de una company
/// Incluye conteos, límites del plan, sesiones activas, etc.
/// </summary>
public record GetCompanyStatsQuery(Guid CompanyId) : IRequest<ApiResponse<CompanyStatsDTO>>;
