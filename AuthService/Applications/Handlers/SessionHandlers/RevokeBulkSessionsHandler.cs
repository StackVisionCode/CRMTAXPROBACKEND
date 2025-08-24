using Commands.SessionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.SessionHandlers;

/// <summary>
/// Handler para revocar múltiples sesiones
/// </summary>
public class RevokeBulkSessionsHandler
    : IRequestHandler<RevokeBulkSessionsCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RevokeBulkSessionsHandler> _logger;

    public RevokeBulkSessionsHandler(
        ApplicationDbContext context,
        ILogger<RevokeBulkSessionsHandler> logger
    )
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        RevokeBulkSessionsCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (!request.SessionIds.Any())
            {
                return new ApiResponse<bool>(false, "No session IDs provided", false);
            }

            // 1. Verificar permisos del usuario solicitante
            var requesterQuery =
                from u in _context.TaxUsers
                where u.Id == request.RequestingUserId && u.IsActive
                select new
                {
                    u.Id,
                    u.CompanyId,
                    u.IsOwner,
                };

            var requester = await requesterQuery.FirstOrDefaultAsync(cancellationToken);

            if (requester == null)
            {
                _logger.LogWarning(
                    "Requesting user not found: {RequesterId}",
                    request.RequestingUserId
                );
                return new ApiResponse<bool>(false, "User not found", false);
            }

            // 2. Obtener sesiones válidas para revocar
            var sessionsQuery =
                from s in _context.Sessions
                join u in _context.TaxUsers on s.TaxUserId equals u.Id
                where
                    request.SessionIds.Contains(s.Id)
                    && u.CompanyId == requester.CompanyId
                    && !s.IsRevoke
                    && (u.Id == request.RequestingUserId || requester.IsOwner) // Solo puede revocar sus propias sesiones o si es owner
                select s;

            var sessionsToRevoke = await sessionsQuery.ToListAsync(cancellationToken);

            if (!sessionsToRevoke.Any())
            {
                _logger.LogWarning(
                    "No valid sessions found to revoke for user: {RequesterId}",
                    request.RequestingUserId
                );
                return new ApiResponse<bool>(false, "No valid sessions found to revoke", false);
            }

            // 3. Revocar todas las sesiones
            var now = DateTime.UtcNow;
            foreach (var session in sessionsToRevoke)
            {
                session.IsRevoke = true;
                session.UpdatedAt = now;
            }

            var result = await _context.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Bulk session revocation successful: {Count} sessions revoked by {RequesterId}, Reason={Reason}",
                    sessionsToRevoke.Count,
                    request.RequestingUserId,
                    request.Reason
                );
                return new ApiResponse<bool>(
                    true,
                    $"{sessionsToRevoke.Count} sessions revoked successfully",
                    true
                );
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(
                    "Failed to revoke bulk sessions for user: {RequesterId}",
                    request.RequestingUserId
                );
                return new ApiResponse<bool>(false, "Failed to revoke sessions", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error revoking bulk sessions for user: {RequesterId}",
                request.RequestingUserId
            );
            return new ApiResponse<bool>(false, "An error occurred while revoking sessions", false);
        }
    }
}
