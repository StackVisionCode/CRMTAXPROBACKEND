using Commands.SessionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.CompanyUserHandlers;

public class CompanyUserLogoutHandler : IRequestHandler<CompanyUserLogoutCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CompanyUserLogoutHandler> _logger;

    public CompanyUserLogoutHandler(
        ApplicationDbContext context,
        ILogger<CompanyUserLogoutHandler> logger
    )
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        CompanyUserLogoutCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var session = await _context.CompanyUserSessions.FirstOrDefaultAsync(
                s => s.Id == request.SessionId && s.CompanyUserId == request.CompanyUserId,
                cancellationToken
            );

            if (session == null)
            {
                _logger.LogWarning(
                    "Logout failed: Session {Session} not found for company user {User}",
                    request.SessionId,
                    request.CompanyUserId
                );
                return new ApiResponse<bool>(false, "Session not found");
            }

            session.IsRevoke = true;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Company user {User} logged out. Session {Session} revoked",
                request.CompanyUserId,
                request.SessionId
            );

            return new ApiResponse<bool>(true, "Logout successful", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error during logout process for company user {User}",
                request.CompanyUserId
            );
            return new ApiResponse<bool>(false, "An error occurred during logout");
        }
    }
}
