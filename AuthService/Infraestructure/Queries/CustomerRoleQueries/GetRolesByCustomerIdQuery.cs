using AuthService.DTOs.RoleDTOs;
using Common;
using MediatR;

namespace Queries.CustomerRoleQueries;

public record GetRolesByCustomerIdQuery(Guid CustomerId) : IRequest<ApiResponse<List<RoleDTO>>>;
