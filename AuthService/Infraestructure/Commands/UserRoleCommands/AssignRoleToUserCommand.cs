using Common;
using MediatR;

namespace Commands.UserRoleCommands;

public record AssignRoleToUserCommand(Guid UserId, Guid RoleId) : IRequest<ApiResponse<bool>>;
