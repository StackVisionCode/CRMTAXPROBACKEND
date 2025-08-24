using AuthService.DTOs.SessionDTOs;
using Common;
using MediatR;

namespace Queries.SessionQueries;

/// <summary>
/// Query para obtener sesiones activas de usuarios de la empresa del solicitante
/// </summary>
public record class GetCompanyActiveSessionsQuery(Guid RequestingUserId)
    : IRequest<ApiResponse<List<SessionWithUserDTO>>>;
