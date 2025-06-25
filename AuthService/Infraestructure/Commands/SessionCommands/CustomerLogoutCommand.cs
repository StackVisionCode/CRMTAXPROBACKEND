using Common;
using MediatR;

namespace Commands.SessionCommands;

public record CustomerLogoutCommand(Guid SessionId, Guid CustomerId) : IRequest<ApiResponse<bool>>;
