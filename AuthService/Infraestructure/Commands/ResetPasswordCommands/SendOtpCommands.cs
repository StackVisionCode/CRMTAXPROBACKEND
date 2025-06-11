using Common;
using MediatR;

namespace AuthService.Commands.ResetPasswordCommands;

public record class SendOtpCommands(string Email, string Token) : IRequest<ApiResponse<Unit>>;
