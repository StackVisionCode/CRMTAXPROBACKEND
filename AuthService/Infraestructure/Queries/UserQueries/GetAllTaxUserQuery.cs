using Common;
using MediatR;
using UserDTOS;

namespace Queries.UserQueries;

public record class GetAllUserQuery:IRequest<ApiResponse<List<UserDTO>>>;