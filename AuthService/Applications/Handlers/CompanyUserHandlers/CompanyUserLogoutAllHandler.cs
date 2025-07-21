using Commands.SessionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.CompanyUserHandlers;

public class CompanyUserLogoutAllHandler
    : IRequestHandler<CompanyUserLogoutAllCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CompanyUserLogoutAllHandler> _logger;

    public CompanyUserLogoutAllHandler(
        ApplicationDbContext context,
        ILogger<CompanyUserLogoutAllHandler> logger
    )
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        CompanyUserLogoutAllCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Buscar todas las sesiones activas del usuario
            var activeSessions = await _context
                .CompanyUserSessions.Where(s =>
                    s.CompanyUserId == request.CompanyUserId && !s.IsRevoke
                )
                .ToListAsync(cancellationToken);

            if (!activeSessions.Any())
            {
                _logger.LogInformation(
                    "No active sessions found for company user {CompanyUserId} to logout",
                    request.CompanyUserId
                );
                return new ApiResponse<bool>(true, "No active sessions found", true);
            }

            // Revocar todas las sesiones
            foreach (var session in activeSessions)
            {
                session.IsRevoke = true;
                session.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "All sessions for company user {CompanyUserId} have been revoked. Total sessions: {Count}",
                request.CompanyUserId,
                activeSessions.Count
            );

            return new ApiResponse<bool>(
                true,
                $"Successfully logged out from {activeSessions.Count} sessions",
                true
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error during logout all process for company user {CompanyUserId}",
                request.CompanyUserId
            );
            return new ApiResponse<bool>(false, "An error occurred during logout process");
        }
    }
}
