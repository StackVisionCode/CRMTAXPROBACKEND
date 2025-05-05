using Common;
using MediatR;

namespace Commands.RolePermissionCommands;

public record class DeleteRolePermissionCommands(int RolePermissionId) : IRequest<ApiResponse<bool>>;