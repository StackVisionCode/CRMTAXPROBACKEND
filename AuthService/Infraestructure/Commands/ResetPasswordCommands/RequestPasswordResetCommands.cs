using Common;
using MediatR;

namespace AuthService.Commands.ResetPasswordCommands;

public record class RequestPasswordResetCommands(string Email, string Origin)
    : IRequest<ApiResponse<Unit>>;
