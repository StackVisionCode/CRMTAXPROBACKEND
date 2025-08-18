using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;

namespace Queries.UserQueries;

// ESTADÍSTICAS DE USUARIOS
public record GetUserStatsQuery(Guid CompanyId) : IRequest<ApiResponse<UserStatsDTO>>;
