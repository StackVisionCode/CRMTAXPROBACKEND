using AuthService.DTOs.InvitationDTOs;
using Common;
using MediatR;

namespace AuthService.Commands.InvitationCommands;

public record class SendUserInvitationCommand(
    NewInvitationDTO Invitation,
    Guid InvitedByUserId,
    string Origin,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<ApiResponse<InvitationDTO>>;
