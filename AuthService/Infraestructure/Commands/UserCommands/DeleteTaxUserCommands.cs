using Common;
using MediatR;
using UserDTOS;

namespace Commands.UserCommands;

public record class DeleteTaxUserCommands(UserDTO Usertax) : IRequest<ApiResponse<bool>>;


