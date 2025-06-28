using AuthService.DTOs.RoleDTOs;
using Common;
using MediatR;

namespace Queries.UserRoleQueries;

public record GetRolesByUserIdQuery(Guid UserId) : IRequest<ApiResponse<List<RoleDTO>>>;
