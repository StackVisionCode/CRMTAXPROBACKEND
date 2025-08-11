using AuthService.DTOs.UserCompanyDTOs;
using Common;
using MediatR;

namespace AuthService.Commands.InvitationCommands;

public record RegisterUserCompanyByInvitationCommand(
    RegisterByInvitationDTO Registration,
    string Origin
) : IRequest<ApiResponse<bool>>;
