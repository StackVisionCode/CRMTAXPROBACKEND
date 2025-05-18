using Common;
using MediatR;

namespace Commands.SessionCommands;

public record class LogoutCommand(string SessionUid, int UserId) : IRequest<ApiResponse<bool>>;