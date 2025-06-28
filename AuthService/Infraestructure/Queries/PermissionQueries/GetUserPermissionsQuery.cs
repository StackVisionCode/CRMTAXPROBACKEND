using AuthService.DTOs.PermissionDTOs;
using Common;
using MediatR;

namespace Queries.PermissionQueries;

public record GetUserPermissionsQuery(Guid UserId) : IRequest<ApiResponse<UserPermissionsDTO>>;
