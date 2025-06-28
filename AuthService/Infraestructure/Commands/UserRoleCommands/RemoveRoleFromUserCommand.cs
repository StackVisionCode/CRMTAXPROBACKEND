using Common;
using MediatR;

namespace Commands.UserRoleCommands;

public record RemoveRoleFromUserCommand(Guid UserId, Guid RoleId) : IRequest<ApiResponse<bool>>;
