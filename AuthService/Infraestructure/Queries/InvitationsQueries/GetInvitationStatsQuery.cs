using AuthService.DTOs.InvitationDTOs;
using Common;
using MediatR;

namespace Infraestructure.Queries.InvitationsQueries;

/// <summary>
/// Query para obtener estadísticas de invitaciones de una company
/// </summary>
public record GetInvitationStatsQuery(Guid CompanyId, int DaysBack = 30)
    : IRequest<ApiResponse<InvitationStatsDTO>>;
