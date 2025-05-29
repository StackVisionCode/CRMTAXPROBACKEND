using AuthService.DTOs.RoleDTOs;
using Common;
using MediatR;

namespace Commands.RolePermissionCommands;

public record class GetRolePermissionByIdQuery(Guid RolePermissionId) : IRequest<ApiResponse<RolePermissionDTO>>;