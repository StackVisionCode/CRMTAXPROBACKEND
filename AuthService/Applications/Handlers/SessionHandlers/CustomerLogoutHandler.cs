using Commands.SessionCommands;
using Common;
using Infraestructure.Context;
using MediatR;

namespace Handlers.SessionHandlers;

public class CustomerLogoutHandler : IRequestHandler<CustomerLogoutCommand, ApiResponse<bool>>
{
    private readonly ILogger<CustomerLogoutHandler> _logger;
    private readonly ApplicationDbContext _context;

    public CustomerLogoutHandler(
        ILogger<CustomerLogoutHandler> logger,
        ApplicationDbContext context
    )
    {
        _logger = logger;
        _context = context;
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

            return new ApiResponse<bool>(true, "Sesión revocada", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al revocar sesión");
            return new ApiResponse<bool>(false, "Error al revocar sesión", false);
        }
    }
}
