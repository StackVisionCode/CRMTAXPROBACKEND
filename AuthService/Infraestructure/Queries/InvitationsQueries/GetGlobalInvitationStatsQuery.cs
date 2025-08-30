using AuthService.DTOs.InvitationDTOs;
using Common;
using MediatR;

namespace Infraestructure.Queries.InvitationsQueries;

/// <summary>
/// Query para obtener estadísticas globales de invitaciones (Developer)
/// </summary>
public record GetGlobalInvitationStatsQuery(int DaysBack = 30)
    : IRequest<ApiResponse<List<InvitationStatsDTO>>>;
