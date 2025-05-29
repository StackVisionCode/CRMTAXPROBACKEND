using Common;
using MediatR;

namespace Commands.PermissionCommands;

public record class DeletePermissionCommands(Guid PermissionId) : IRequest<ApiResponse<bool>>;