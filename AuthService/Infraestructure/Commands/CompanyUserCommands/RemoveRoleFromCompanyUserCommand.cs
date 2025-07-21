using Common;
using MediatR;

namespace Commands.CompanyUserCommands;

public record class RemoveRoleFromCompanyUserCommand(Guid CompanyUserId, Guid RoleId)
    : IRequest<ApiResponse<bool>>;
