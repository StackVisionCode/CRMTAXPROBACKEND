using AuthService.DTOs.RoleDTOs;
using Common;
using MediatR;

namespace Commands.RolePermissionCommands;

public record class GetAllRolePermissionQuery : IRequest<ApiResponse<List<RolePermissionDTO>>>;