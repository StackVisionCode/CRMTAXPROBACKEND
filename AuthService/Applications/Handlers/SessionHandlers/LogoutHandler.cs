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
            // Buscar sesión del TaxUser
            var session = await _context
                .Sessions.Where(s => s.Id == request.SessionId && s.TaxUserId == request.UserId)
                .FirstOrDefaultAsync(cancellationToken);

            if (session == null)
            {
                _logger.LogWarning(
                    "Logout failed: Session {SessionId} not found for TaxUser {UserId}",
                    request.SessionId,
                    request.UserId
                );
                return new ApiResponse<bool>(false, "Session not found");
            }

            // Verificar si la sesión ya está revocada
            if (session.IsRevoke)
            {
                _logger.LogInformation(
                    "Session {SessionId} for TaxUser {UserId} was already revoked",
                    request.SessionId,
                    request.UserId
                );
                return new ApiResponse<bool>(true, "Session already logged out", true);
            }

            // Revocar la sesión
            session.IsRevoke = true;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "TaxUser {UserId} logged out successfully. Session {SessionId} revoked",
                request.UserId,
                request.SessionId
            );

            return new ApiResponse<bool>(true, "Logout successful", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error during logout process for TaxUser {UserId}, Session {SessionId}",
                request.UserId,
                request.SessionId
            );
            return new ApiResponse<bool>(false, "An error occurred during logout");
        }
    }
}
