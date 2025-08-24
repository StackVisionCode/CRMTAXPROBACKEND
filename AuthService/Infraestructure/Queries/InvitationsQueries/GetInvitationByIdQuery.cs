using AuthService.DTOs.InvitationDTOs;
using Common;
using MediatR;

namespace Infraestructure.Queries.InvitationsQueries;

/// <summary>
/// Query para obtener una invitación por ID
/// </summary>
public record GetInvitationByIdQuery(Guid InvitationId, Guid? RequestingUserId = null)
    : IRequest<ApiResponse<InvitationDTO>>;
