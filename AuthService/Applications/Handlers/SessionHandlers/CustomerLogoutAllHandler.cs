using Commands.SessionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class CustomerLogoutAllHandler : IRequestHandler<CustomerLogoutAllCommand, ApiResponse<bool>>
{
    private readonly ILogger<CustomerLogoutAllHandler> _logger;
    private readonly ApplicationDbContext _context;

    public CustomerLogoutAllHandler(
        ILogger<CustomerLogoutAllHandler> logger,
        ApplicationDbContext context
    )
    {
        _logger = logger;
        _context = context;
    }

    public async Task<ApiResponse<bool>> Handle(
        CustomerLogoutAllCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var q = _context.CustomerSessions.Where(s =>
                s.CustomerId == request.CustomerId && !s.IsRevoke
            );
            await q.ExecuteUpdateAsync(
                s => s.SetProperty(p => p.IsRevoke, true),
                cancellationToken
            );

            return new ApiResponse<bool>(true, "Sesiones revocadas", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al revocar sesiones: {Message}", ex.Message);
            return new ApiResponse<bool>(false, "Error al revocar sesiones", false);
        }
    }
}
