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
            var totalRevoked = 0;

            // Revocar sesiones de TaxUser
            var taxUserSessions = await _context
                .Sessions.Where(s => s.TaxUserId == request.UserId && !s.IsRevoke)
                .ToListAsync(cancellationToken);

            foreach (var session in taxUserSessions)
            {
                session.IsRevoke = true;
                session.UpdatedAt = DateTime.UtcNow;
            }
            totalRevoked += taxUserSessions.Count;

            // Revocar sesiones de UserCompany
            var userCompanySessions = await _context
                .UserCompanySessions.Where(s => s.UserCompanyId == request.UserId && !s.IsRevoke)
                .ToListAsync(cancellationToken);

            foreach (var session in userCompanySessions)
            {
                session.IsRevoke = true;
                session.UpdatedAt = DateTime.UtcNow;
            }
            totalRevoked += userCompanySessions.Count;

            if (totalRevoked == 0)
            {
                _logger.LogInformation(
                    "No active sessions found for user {UserId}",
                    request.UserId
                );
                return new ApiResponse<bool>(true, "No active sessions found", true);
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "All sessions for user {UserId} revoked. Total: {Count}",
                request.UserId,
                totalRevoked
            );

            return new ApiResponse<bool>(
                true,
                $"Successfully logged out from {totalRevoked} sessions",
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
