using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.SessionQueries;

namespace Handlers.SessionHandlers;

public class ValidateSessionHandler : IRequestHandler<ValidateSessionQuery, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ValidateSessionHandler> _logger;

    public ValidateSessionHandler(ApplicationDbContext context, ILogger<ValidateSessionHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(ValidateSessionQuery request, CancellationToken cancellationToken)
    {
        try
        {
            return await _context.Sessions
            .AnyAsync(s => s.SessionUid == request.SessionUid &&
                        !s.IsRevoke &&
                        s.ExpireTokenRequest > DateTime.UtcNow, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating session with UID: {SessionUid}", request.SessionUid);
            return false;
        }
    }
}
