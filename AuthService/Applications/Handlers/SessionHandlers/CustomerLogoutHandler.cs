using Commands.SessionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CommEvents.IdentityEvents;

namespace Handlers.SessionHandlers;

public class CustomerLogoutHandler : IRequestHandler<CustomerLogoutCommand, ApiResponse<bool>>
{
    private readonly ILogger<CustomerLogoutHandler> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IEventBus _eventBus;

    public CustomerLogoutHandler(
        ILogger<CustomerLogoutHandler> logger,
        ApplicationDbContext context,
        IEventBus eventBus
    )
    {
        _logger = logger;
        _context = context;
        _eventBus = eventBus;
    }

    public async Task<ApiResponse<bool>> Handle(
        CustomerLogoutCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var sess = await _context.CustomerSessions.FindAsync(
                [request.SessionId],
                cancellationToken
            );
            if (sess is null || sess.IsRevoke)
                return new(false, "Sesión no encontrada", false);

            sess.IsRevoke = true;
            await _context.SaveChangesAsync(cancellationToken);

            _eventBus.Publish(
                new UserPresenceChangedEvent(
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    sess.CustomerId,
                    "Customer",
                    false
                )
            );

            return new ApiResponse<bool>(true, "Sesión revocada", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al revocar sesión");
            return new ApiResponse<bool>(false, "Error al revocar sesión", false);
        }
    }
}
