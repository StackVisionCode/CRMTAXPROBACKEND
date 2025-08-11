using Commands.SessionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;

namespace Handlers.SessionHandlers;

public class LogoutHandler : IRequestHandler<LogoutCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<LogoutHandler> _logger;
    private readonly IEventBus _eventBus;

    public LogoutHandler(
        ApplicationDbContext context,
        ILogger<LogoutHandler> logger,
        IEventBus eventBus
    )
    {
        _context = context;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<ApiResponse<bool>> Handle(
        LogoutCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Buscar en Sessions (TaxUsers)
            var taxUserSession = await _context.Sessions.FirstOrDefaultAsync(
                s => s.Id == request.SessionId && s.TaxUserId == request.UserId,
                cancellationToken
            );

            if (taxUserSession != null)
            {
                taxUserSession.IsRevoke = true;
                taxUserSession.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "TaxUser {UserId} logged out. Session {SessionId} revoked",
                    request.UserId,
                    request.SessionId
                );
                return new ApiResponse<bool>(true, "Logout successful", true);
            }

            // Buscar en UserCompanySessions
            var userCompanySession = await _context.UserCompanySessions.FirstOrDefaultAsync(
                s => s.Id == request.SessionId && s.UserCompanyId == request.UserId,
                cancellationToken
            );

            if (userCompanySession != null)
            {
                userCompanySession.IsRevoke = true;
                userCompanySession.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "UserCompany {UserId} logged out. Session {SessionId} revoked",
                    request.UserId,
                    request.SessionId
                );
                return new ApiResponse<bool>(true, "Logout successful", true);
            }

            _logger.LogWarning(
                "Logout failed: Session {SessionId} not found for user {UserId}",
                request.SessionId,
                request.UserId
            );
            return new ApiResponse<bool>(false, "Session not found");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout process for user {UserId}", request.UserId);
            return new ApiResponse<bool>(false, "An error occurred during logout");
        }
    }
}
