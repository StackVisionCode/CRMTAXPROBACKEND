using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;

namespace AuthService.Commands.InvitationCommands;

public record ValidateInvitationCommand(string Token)
    : IRequest<ApiResponse<InvitationValidationDTO>>;
