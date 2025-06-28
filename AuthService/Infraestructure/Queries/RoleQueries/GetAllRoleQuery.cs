using AuthService.DTOs.RoleDTOs;
using Common;
using MediatR;

namespace Queries.RoleQueries;

public record class GetAllRoleQuery : IRequest<ApiResponse<List<RoleDTO>>>;
