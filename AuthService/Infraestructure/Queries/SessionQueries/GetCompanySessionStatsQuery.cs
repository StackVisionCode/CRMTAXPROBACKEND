using AuthService.DTOs.SessionDTOs;
using Common;
using MediatR;

namespace Queries.SessionQueries;

/// <summary>
/// Query para estadísticas de sesiones de la empresa
/// </summary>
public record class GetCompanySessionStatsQuery(Guid RequestingUserId, string TimeRange)
    : IRequest<ApiResponse<CompanySessionStatsDTO>>;
