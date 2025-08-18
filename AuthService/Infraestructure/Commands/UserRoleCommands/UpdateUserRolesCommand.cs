using Common;
using MediatR;

namespace Commands.UserRoleCommands;

public record UpdateUserRolesCommand(Guid UserId, IEnumerable<Guid> RoleIds)
    : IRequest<ApiResponse<bool>>;
