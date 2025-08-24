using Commands.SessionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.SessionHandlers;

/// <summary>
/// Handler para revocar una sesión específica
/// </summary>
public class RevokeSessionHandler : IRequestHandler<RevokeSessionCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RevokeSessionHandler> _logger;

    public RevokeSessionHandler(ApplicationDbContext context, ILogger<RevokeSessionHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        RevokeSessionCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Verificar que la sesión existe y obtener información del usuario
            var sessionQuery =
                from s in _context.Sessions
                join u in _context.TaxUsers on s.TaxUserId equals u.Id
                join requester in _context.TaxUsers on u.CompanyId equals requester.CompanyId
                where
                    s.Id == request.SessionId
                    && requester.Id == request.RequestingUserId
                    && !s.IsRevoke
                select new
                {
                    Session = s,
                    TargetUser = u,
                    Requester = requester,
                };

            var sessionInfo = await sessionQuery.FirstOrDefaultAsync(cancellationToken);

            if (sessionInfo == null)
            {
                _logger.LogWarning(
                    "Session not found or unauthorized access: SessionId={SessionId}, RequesterId={RequesterId}",
                    request.SessionId,
                    request.RequestingUserId
                );
                return new ApiResponse<bool>(
                    false,
                    "Session not found or you don't have permission to revoke it",
                    false
                );
            }

            // 2. Verificar permisos: solo owners pueden revocar sesiones de otros usuarios
            if (
                sessionInfo.TargetUser.Id != request.RequestingUserId
                && !sessionInfo.Requester.IsOwner
            )
            {
                _logger.LogWarning(
                    "Insufficient permissions to revoke session: RequesterId={RequesterId}, TargetUserId={TargetUserId}",
                    request.RequestingUserId,
                    sessionInfo.TargetUser.Id
                );
                return new ApiResponse<bool>(
                    false,
                    "You don't have permission to revoke this session",
                    false
                );
            }

            // 3. Revocar la sesión
            sessionInfo.Session.IsRevoke = true;
            sessionInfo.Session.UpdatedAt = DateTime.UtcNow;

            var result = await _context.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Session revoked successfully: SessionId={SessionId}, TargetUserId={TargetUserId}, RequesterId={RequesterId}, Reason={Reason}",
                    request.SessionId,
                    sessionInfo.TargetUser.Id,
                    request.RequestingUserId,
                    request.Reason
                );
                return new ApiResponse<bool>(true, "Session revoked successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(
                    "Failed to revoke session: SessionId={SessionId}",
                    request.SessionId
                );
                return new ApiResponse<bool>(false, "Failed to revoke session", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error revoking session: SessionId={SessionId}",
                request.SessionId
            );
            return new ApiResponse<bool>(
                false,
                "An error occurred while revoking the session",
                false
            );
        }
    }
}
