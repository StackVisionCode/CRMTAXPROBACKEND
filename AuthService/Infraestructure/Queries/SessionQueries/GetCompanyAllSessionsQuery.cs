using AuthService.DTOs.SessionDTOs;
using Common;
using MediatR;

namespace Queries.SessionQueries;

/// <summary>
/// Query para obtener todas las sesiones de usuarios de la empresa del solicitante
/// </summary>
public record class GetCompanyAllSessionsQuery(Guid RequestingUserId)
    : IRequest<ApiResponse<List<SessionDTO>>>;
