using AuthService.DTOs.InvitationDTOs;
using Common;
using MediatR;

namespace Infraestructure.Queries.InvitationsQueries;

/// <summary>
/// Query para verificar si una company puede enviar más invitaciones
/// </summary>
public record CanSendMoreInvitationsQuery(Guid CompanyId)
    : IRequest<ApiResponse<InvitationLimitCheckDTO>>;
