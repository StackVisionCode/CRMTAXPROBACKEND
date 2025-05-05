using AuthService.DTOs.RoleDTOs;
using Common;
using MediatR;

namespace Commands.RolePermissionCommands;

public record class UpdateRolePermissionCommands(RolePermissionDTO rolePermission) : IRequest<ApiResponse<bool>>;