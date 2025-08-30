using AuthService.DTOs.InvitationDTOs;
using Common;
using MediatR;

namespace AuthService.Commands.InvitationCommands;

/// <summary>
/// Command para cancelar una invitación
/// </summary>
public record class CancelInvitationCommand(
    CancelInvitationDTO CancelRequest,
    Guid CancelledByUserId
) : IRequest<ApiResponse<bool>>;
