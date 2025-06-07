using Common;
using MediatR;

namespace AuthService.Commands.ResetPasswordCommands;

public record class ValidateOtpCommands(string Email, string Otp) : IRequest<ApiResponse<Unit>>;
