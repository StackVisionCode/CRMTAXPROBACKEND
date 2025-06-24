using Commands.SessionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CommEvents.IdentityEvents;

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
            var session = await _context.Sessions.FirstOrDefaultAsync(
                s => s.Id == request.SessionId && s.TaxUserId == request.UserId,
                cancellationToken
            );

            if (session == null)
            {
                _logger.LogWarning(
                    "Logout failed: Session {Session} not found for user {User}",
                    request.SessionId,
                    request.UserId
                );
                return new ApiResponse<bool>(false, "Session not found");
            }

            // Revocar la sesión
            session.IsRevoke = true;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            // Publicar evento de logout
            _eventBus.Publish(
                new UserPresenceChangedEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    request.UserId,
                    "TaxUser",
                    false
                )
            );

            _logger.LogInformation(
                "User {User} logged out. Session {Session} revoked",
                request.UserId,
                request.SessionId
            );
            return new ApiResponse<bool>(true, "Logout successful", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout process for user {User}", request.UserId);
            return new ApiResponse<bool>(false, "An error occurred during logout");
        }
    }
}
