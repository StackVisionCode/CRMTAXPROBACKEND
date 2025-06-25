using Common;
using MediatR;

namespace Commands.SessionCommands;

public record CustomerLogoutAllCommand(Guid CustomerId) : IRequest<ApiResponse<bool>>;
