using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SessionQueries;

namespace Handlers.SessionHandlers;

public class ValidateSessionHandler : IRequestHandler<ValidateSessionQuery, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ValidateSessionHandler> _logger;

    public ValidateSessionHandler(
        ApplicationDbContext context,
        ILogger<ValidateSessionHandler> logger
    )
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(
        ValidateSessionQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1) comprobar en tabla de usuarios preparadores
            var ok =
                await _context.Sessions.AnyAsync(
                    s =>
                        s.Id == request.SessionId
                        && !s.IsRevoke
                        && s.ExpireTokenRequest > DateTime.UtcNow,
                    cancellationToken
                )
                // 2) o en tabla de clientes
                || await _context.CustomerSessions.AnyAsync(
                    s =>
                        s.Id == request.SessionId
                        && !s.IsRevoke
                        && s.ExpireTokenRequest > DateTime.UtcNow,
                    cancellationToken
                );

            return ok;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error validating session with UID: {SessionId}",
                request.SessionId
            );
            return false;
        }
    }
}
