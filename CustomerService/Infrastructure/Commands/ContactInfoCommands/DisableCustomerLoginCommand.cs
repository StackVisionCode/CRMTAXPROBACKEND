using Common;
using MediatR;

namespace CustomerService.Commands.ContactInfoCommands;

public record class DisableCustomerLoginCommand(Guid CustomerId) : IRequest<ApiResponse<bool>>;
