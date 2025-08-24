using Common;
using MediatR;

namespace Commands.SessionCommands;

/// <summary>
/// Command para revocar múltiples sesiones
/// </summary>
public record class RevokeBulkSessionsCommand(
    Guid RequestingUserId,
    List<Guid> SessionIds,
    string? Reason
) : IRequest<ApiResponse<bool>>;
