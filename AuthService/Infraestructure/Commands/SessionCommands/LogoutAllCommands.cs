using Common;
using MediatR;

namespace Commands.SessionCommands;

public record class LogoutAllCommands(int UserId) : IRequest<ApiResponse<bool>>;