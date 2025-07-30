using Common;
using MediatR;

namespace Commands.SessionCommands;

public record class LogoutAllCommands(Guid UserId) : IRequest<ApiResponse<bool>>;
