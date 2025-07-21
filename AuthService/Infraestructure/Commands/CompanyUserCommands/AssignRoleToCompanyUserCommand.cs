using Common;
using MediatR;

namespace Commands.CompanyUserCommands;

public record class AssignRoleToCompanyUserCommand(Guid CompanyUserId, Guid RoleId)
    : IRequest<ApiResponse<bool>>;
