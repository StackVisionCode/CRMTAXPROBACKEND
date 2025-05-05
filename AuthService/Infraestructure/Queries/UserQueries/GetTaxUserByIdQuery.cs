using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;

namespace Queries.UserQueries;

public record class GetTaxUserByIdQuery(int Id) : IRequest<ApiResponse<UserGetDTO>>;
