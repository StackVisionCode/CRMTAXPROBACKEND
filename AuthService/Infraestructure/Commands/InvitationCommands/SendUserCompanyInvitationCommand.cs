using AuthService.DTOs.UserCompanyDTOs;
using Common;
using MediatR;

namespace AuthService.Commands.InvitationCommands;

public record SendUserCompanyInvitationCommand(SendInvitationDTO Invitation, string Origin)
    : IRequest<ApiResponse<bool>>;
