using Common;
using MediatR;

namespace Commands.SessionCommands;

/// <summary>
/// Command para revocar todas las sesiones de un usuario
/// </summary>
public record class RevokeUserSessionsCommand(
    Guid RequestingUserId,
    Guid TargetUserId,
    string? Reason
) : IRequest<ApiResponse<bool>>;
