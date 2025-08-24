using Commands.SessionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.SessionHandlers;

/// <summary>
/// Handler para revocar todas las sesiones de un usuario
/// </summary>
public class RevokeUserSessionsHandler
    : IRequestHandler<RevokeUserSessionsCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RevokeUserSessionsHandler> _logger;

    public RevokeUserSessionsHandler(
        ApplicationDbContext context,
        ILogger<RevokeUserSessionsHandler> logger
    )
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        RevokeUserSessionsCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Verificar que ambos usuarios pertenecen a la misma empresa y permisos
            var usersQuery =
                from requester in _context.TaxUsers
                join target in _context.TaxUsers on requester.CompanyId equals target.CompanyId
                where
                    requester.Id == request.RequestingUserId
                    && target.Id == request.TargetUserId
                    && requester.IsActive
                    && target.IsActive
                select new { Requester = requester, Target = target };

            var usersInfo = await usersQuery.FirstOrDefaultAsync(cancellationToken);

            if (usersInfo == null)
            {
                _logger.LogWarning(
                    "Users not found or don't belong to same company: Requester={RequesterId}, Target={TargetUserId}",
                    request.RequestingUserId,
                    request.TargetUserId
                );
                return new ApiResponse<bool>(
                    false,
                    "Users not found or don't belong to the same company",
                    false
                );
            }

            // 2. Verificar permisos: solo owners pueden revocar sesiones de otros usuarios
            if (request.TargetUserId != request.RequestingUserId && !usersInfo.Requester.IsOwner)
            {
                _logger.LogWarning(
                    "Insufficient permissions to revoke user sessions: RequesterId={RequesterId}, TargetUserId={TargetUserId}",
                    request.RequestingUserId,
                    request.TargetUserId
                );
                return new ApiResponse<bool>(
                    false,
                    "You don't have permission to revoke sessions for this user",
                    false
                );
            }

            // 3. Obtener todas las sesiones activas del usuario objetivo
            var sessionsQuery =
                from s in _context.Sessions
                where s.TaxUserId == request.TargetUserId && !s.IsRevoke
                select s;

            var sessionsToRevoke = await sessionsQuery.ToListAsync(cancellationToken);

            if (!sessionsToRevoke.Any())
            {
                _logger.LogInformation(
                    "No active sessions found for user: {TargetUserId}",
                    request.TargetUserId
                );
                return new ApiResponse<bool>(true, "No active sessions found to revoke", true);
            }

            // 4. Revocar todas las sesiones
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
                    "All user sessions revoked successfully: {Count} sessions for TargetUserId={TargetUserId} by RequesterId={RequesterId}, Reason={Reason}",
                    sessionsToRevoke.Count,
                    request.TargetUserId,
                    request.RequestingUserId,
                    request.Reason
                );
                return new ApiResponse<bool>(
                    true,
                    $"All {sessionsToRevoke.Count} user sessions revoked successfully",
                    true
                );
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(
                    "Failed to revoke user sessions: TargetUserId={TargetUserId}",
                    request.TargetUserId
                );
                return new ApiResponse<bool>(false, "Failed to revoke user sessions", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error revoking user sessions: TargetUserId={TargetUserId}, RequesterId={RequesterId}",
                request.TargetUserId,
                request.RequestingUserId
            );
            return new ApiResponse<bool>(
                false,
                "An error occurred while revoking user sessions",
                false
            );
        }
    }
}
