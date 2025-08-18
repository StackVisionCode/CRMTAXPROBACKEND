using Common;
using MediatR;

namespace Commands.UserCommands;

// RESET PASSWORD
public record ResetUserPasswordCommand(Guid UserId, string NewPassword)
    : IRequest<ApiResponse<bool>>;
