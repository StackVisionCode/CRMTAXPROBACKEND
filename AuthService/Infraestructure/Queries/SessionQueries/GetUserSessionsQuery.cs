using AuthService.DTOs.SessionDTOs;
using Common;
using MediatR;

namespace Queries.SessionQueries;

/// <summary>
/// Query para obtener sesiones de un usuario específico (solo si pertenece a la misma empresa)
/// </summary>
public record class GetUserSessionsQuery(Guid RequestingUserId, Guid TargetUserId)
    : IRequest<ApiResponse<List<SessionWithUserDTO>>>;
