using AuthService.DTOs.RoleDTOs;
using Common;
using MediatR;

namespace Queries.RoleQueries;

public record class GetRoleByIdQuery(int RoleId) : IRequest<ApiResponse<RoleDTO>>;