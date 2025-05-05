using AuthService.DTOs.RoleDTOs;
using Common;
using MediatR;

namespace Commands.RolePermissionCommands;

public record class GetRolePermissionByIdQuery(int RolePermissionId) : IRequest<ApiResponse<RolePermissionDTO>>;