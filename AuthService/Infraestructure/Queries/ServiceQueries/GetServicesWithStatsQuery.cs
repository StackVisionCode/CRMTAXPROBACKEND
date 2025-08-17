using AuthService.DTOs.ServiceDTOs;
using Common;
using MediatR;

namespace Queries.ServiceQueries;

/// <summary>
/// Obtener Services con estadísticas de uso
/// (Cuántas Companies usan cada Service)
/// </summary>
public record GetServicesWithStatsQuery() : IRequest<ApiResponse<IEnumerable<ServiceWithStatsDTO>>>;
