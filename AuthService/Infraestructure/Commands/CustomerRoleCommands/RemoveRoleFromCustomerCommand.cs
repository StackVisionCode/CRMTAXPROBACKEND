using Common;
using MediatR;

namespace Commands.CustomerRoleCommands;

public record RemoveRoleFromCustomerCommand(Guid CustomerId, Guid RoleId)
    : IRequest<ApiResponse<bool>>;
