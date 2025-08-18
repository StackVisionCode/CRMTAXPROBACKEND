using Common;
using MediatR;

namespace Commands.UserCommands;

public record class EnableUserCommand(Guid UserId) : IRequest<ApiResponse<bool>>;
