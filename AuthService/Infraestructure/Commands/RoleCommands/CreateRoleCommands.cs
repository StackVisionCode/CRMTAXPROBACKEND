using AuthService.DTOs.RoleDTOs;
using Common;
using MediatR;

namespace Commands.RoleCommands;
public record class CreateRoleCommands(RoleDTO Role) : IRequest<ApiResponse<bool>>;