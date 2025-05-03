using Common;
using MediatR;
using UserDTOS;

namespace Commands.UserCommands;

public record class UpdateTaxUserCommands(UserDTO Usertax) : IRequest<ApiResponse<bool>>;