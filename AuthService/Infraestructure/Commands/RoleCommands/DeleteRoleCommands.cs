using Common;
using MediatR;

namespace Commands.RoleCommands;

public record class DeleteRoleCommands(Guid RoleId) : IRequest<ApiResponse<bool>>;
