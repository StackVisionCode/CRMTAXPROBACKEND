using Commands.SessionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

namespace Handlers.SessionHandlers;

public class LogoutAllHandler : IRequestHandler<LogoutAllCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LogoutAllHandler> _logger;
    private readonly IEventBus _eventBus;

    public LogoutAllHandler(
        ApplicationDbContext context,
        ILogger<LogoutAllHandler> logger,
        IEventBus eventBus
    )
    {
        _context = context;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<ApiResponse<bool>> Handle(
        LogoutAllCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Buscar todas las sesiones activas del usuario
            var activeSessions = await _context
                .Sessions.Where(s => s.TaxUserId == request.UserId && !s.IsRevoke)
                .ToListAsync(cancellationToken);

            if (!activeSessions.Any())
            {
                _logger.LogInformation(
                    "No active sessions found for user {UserId} to logout",
                    request.UserId
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
                "All sessions for user {UserId} have been revoked. Total sessions: {Count}",
                request.UserId,
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
                "Error during logout all process for user {UserId}",
                request.UserId
            );
            return new ApiResponse<bool>(false, "An error occurred during logout process");
        }
    }
}
