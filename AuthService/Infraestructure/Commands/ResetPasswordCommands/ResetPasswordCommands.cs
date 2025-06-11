using Common;
using MediatR;

namespace AuthService.Commands.ResetPasswordCommands;

public record class ResetPasswordCommands(string Email, string NewPassword, string Token)
    : IRequest<ApiResponse<Unit>>;
