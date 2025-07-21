using AuthService.DTOs.RoleDTOs;
using Common;
using MediatR;

namespace Queries.CompanyUserQueries;

public record class GetRolesByCompanyUserIdQuery(Guid CompanyUserId)
    : IRequest<ApiResponse<List<RoleDTO>>>;
