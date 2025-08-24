using Common;
using MediatR;

namespace Commands.SessionCommands;

/// <summary>
/// Command para revocar una sesión específica
/// </summary>
public record class RevokeSessionCommand(Guid RequestingUserId, Guid SessionId, string? Reason)
    : IRequest<ApiResponse<bool>>;
