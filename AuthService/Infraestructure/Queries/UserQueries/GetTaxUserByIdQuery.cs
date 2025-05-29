using AuthService.DTOs.UserDTOs;
using Common;
using MediatR;

namespace Queries.UserQueries;

public record class GetTaxUserByIdQuery(Guid Id) : IRequest<ApiResponse<UserGetDTO>>;
