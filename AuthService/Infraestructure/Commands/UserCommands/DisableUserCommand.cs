using Common;
using MediatR;

namespace Commands.UserCommands;

public record class DisableUserCommand(Guid UserId) : IRequest<ApiResponse<bool>>;
