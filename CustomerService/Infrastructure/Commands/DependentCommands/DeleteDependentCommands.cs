using Common;
using MediatR;

namespace CustomerService.Commands.DependentCommands;

public record class DeleteDependentCommands(Guid Id) : IRequest<ApiResponse<bool>>;
