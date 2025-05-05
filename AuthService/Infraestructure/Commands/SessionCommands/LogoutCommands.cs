using Common;
using MediatR;

namespace Commands.SessionCommands;

public record class LogoutCommand(int SessionId, int UserId) : IRequest<ApiResponse<bool>>;