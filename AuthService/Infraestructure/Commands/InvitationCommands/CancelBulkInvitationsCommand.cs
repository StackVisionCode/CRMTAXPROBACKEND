using AuthService.DTOs.InvitationDTOs;
using Common;
using MediatR;

namespace AuthService.Commands.InvitationCommands;

/// <summary>
/// Command para cancelar múltiples invitaciones
/// </summary>
public record CancelBulkInvitationsCommand(
    CancelBulkInvitationsDTO CancelRequest,
    Guid CancelledByUserId
) : IRequest<ApiResponse<int>>; // Retorna cantidad cancelada
