using Common;
using MediatR;

namespace Commands.RoleCommands;

public record class DeleteRoleCommands(int RoleId) : IRequest<ApiResponse<bool>>;