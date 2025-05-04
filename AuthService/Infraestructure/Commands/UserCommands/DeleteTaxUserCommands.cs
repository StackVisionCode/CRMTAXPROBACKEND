using Common;
using MediatR;
using UserDTOS;

namespace Commands.UserCommands;

public record class DeleteTaxUserCommands(int Id) : IRequest<ApiResponse<bool>>;


