using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;

namespace AuthService.Commands.InvitationCommands;

public record RegisterUserByInvitationCommand(RegisterByInvitationDTO Registration, string Origin)
    : IRequest<ApiResponse<bool>>;
