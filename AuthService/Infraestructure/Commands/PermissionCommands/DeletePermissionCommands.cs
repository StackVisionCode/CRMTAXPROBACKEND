using Common;
using MediatR;

namespace Commands.PermissionCommands;

public record class DeletePermissionCommands(int PermissionId) : IRequest<ApiResponse<bool>>;