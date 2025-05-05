using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;

namespace Queries.UserQueries;

public record GetTaxUserProfileQuery(int UserId) : IRequest<ApiResponse<UserProfileDTO>>;