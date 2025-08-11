using AuthService.DTOs.UserCompanyDTOs;
using Common;
using MediatR;

namespace AuthService.Commands.InvitationCommands;

public record ValidateInvitationCommand(string Token)
    : IRequest<ApiResponse<InvitationValidationDTO>>;
