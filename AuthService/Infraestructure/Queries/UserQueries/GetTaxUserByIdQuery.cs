using Common;
using MediatR;
using UserDTOS;

namespace Queries.UserQueries;

public record class GetTaxUserByIdQuery(int  UsertaxId) : IRequest<ApiResponse<UserDTO>>;
