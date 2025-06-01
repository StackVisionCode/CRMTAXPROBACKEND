using Common;
using MediatR;

namespace CustomerService.Commands.DependentCommands;

public record class DeleteDependetCommands(Guid Id) : IRequest<ApiResponse<bool>>;
