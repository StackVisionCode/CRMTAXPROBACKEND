using Common;
using MediatR;

namespace Commands.SessionCommands;

public record class LogoutCommand(Guid SessionId, Guid UserId) : IRequest<ApiResponse<bool>>;
