using Common;
using MediatR;

namespace Commands.RolePermissionCommands;

public record class DeleteRolePermissionCommands(Guid RolePermissionId) : IRequest<ApiResponse<bool>>;