using AuthService.DTOs.RoleDTOs;
using Common;
using MediatR;

namespace Queries.RoleQueries;

public record class GetRoleByIdQuery(Guid RoleId) : IRequest<ApiResponse<RoleDTO>>;
