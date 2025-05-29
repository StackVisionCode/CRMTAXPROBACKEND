using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;

namespace Queries.UserQueries;

public record GetTaxUserProfileQuery(Guid UserId) : IRequest<ApiResponse<UserProfileDTO>>;