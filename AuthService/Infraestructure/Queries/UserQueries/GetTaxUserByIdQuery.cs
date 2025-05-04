using Common;
using MediatR;
using UserDTOS;

namespace Queries.UserQueries;

public record class GetTaxUserByIdQuery(int Id) : IRequest<ApiResponse<UserDTO>>;
