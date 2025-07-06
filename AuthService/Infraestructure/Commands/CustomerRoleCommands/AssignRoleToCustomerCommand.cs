using Common;
using MediatR;

namespace Commands.CustomerRoleCommands;

public record AssignRoleToCustomerCommand(Guid CustomerId, Guid RoleId)
    : IRequest<ApiResponse<bool>>;
