using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;

namespace Queries.UserQueries;

public record class GetAllUserQuery:IRequest<ApiResponse<List<UserGetDTO>>>;