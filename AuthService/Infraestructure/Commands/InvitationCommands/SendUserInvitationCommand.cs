using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;

namespace AuthService.Commands.InvitationCommands;

public record SendUserInvitationCommand(SendInvitationDTO Invitation, string Origin)
    : IRequest<ApiResponse<bool>>;
