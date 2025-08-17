using Common;
using MediatR;

namespace Commands.UserCommands;

public record class DeleteTaxUserCommands(Guid UserId) : IRequest<ApiResponse<bool>>;
