using Common;
using MediatR;

namespace Commands.UserCommands;

public record class DeleteTaxUserCommands(int Id) : IRequest<ApiResponse<bool>>;


