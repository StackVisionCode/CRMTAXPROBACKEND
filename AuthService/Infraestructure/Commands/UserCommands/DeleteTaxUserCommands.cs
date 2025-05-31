using Common;
using MediatR;

namespace Commands.UserCommands;

public record class DeleteTaxUserCommands(Guid Id) : IRequest<ApiResponse<bool>>;
