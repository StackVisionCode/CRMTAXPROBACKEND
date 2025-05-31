using Common;
using MediatR;

namespace CustomerService.Commands.CustomerCommands;

public record class DeleteCustomerCommands(Guid Id) : IRequest<ApiResponse<bool>>;
