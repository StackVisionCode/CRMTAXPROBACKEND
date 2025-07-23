using Commands.CompanyUserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.CompanyUserHandlers;

public class DeleteCompanyUserHandler : IRequestHandler<DeleteCompanyUserCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteCompanyUserHandler> _logger;

    public DeleteCompanyUserHandler(
        ApplicationDbContext dbContext,
        ILogger<DeleteCompanyUserHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteCompanyUserCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // PASO 1: Verificar existencia del usuario SIN Include
            var userExists = await _dbContext
                .CompanyUsers.Where(cu => cu.Id == request.CompanyUserId)
                .Select(cu => new { cu.Id, cu.Email }) // Solo los datos necesarios
                .FirstOrDefaultAsync(cancellationToken);

            if (userExists is null)
            {
                _logger.LogWarning(
                    "Company user not found: {CompanyUserId}",
                    request.CompanyUserId
                );
                return new ApiResponse<bool>(false, "Company user not found", false);
            }

            // PASO 2: Revocar sesiones activas usando UPDATE directo
            var sessionsUpdated = await _dbContext
                .CompanyUserSessions.Where(s =>
                    s.CompanyUserId == request.CompanyUserId && !s.IsRevoke
                )
                .ExecuteUpdateAsync(
                    s =>
                        s.SetProperty(session => session.IsRevoke, true)
                            .SetProperty(session => session.UpdatedAt, DateTime.UtcNow),
                    cancellationToken
                );

            // PASO 3: Soft delete del usuario usando UPDATE directo
            var userUpdated = await _dbContext
                .CompanyUsers.Where(cu => cu.Id == request.CompanyUserId)
                .ExecuteUpdateAsync(
                    u =>
                        u.SetProperty(user => user.DeleteAt, DateTime.UtcNow)
                            .SetProperty(user => user.IsActive, false)
                            .SetProperty(user => user.UpdatedAt, DateTime.UtcNow),
                    cancellationToken
                );

            var success = userUpdated > 0;

            if (success)
            {
                _logger.LogInformation(
                    "Company user deleted successfully: {CompanyUserId}. Sessions revoked: {SessionsCount}",
                    request.CompanyUserId,
                    sessionsUpdated
                );
            }

            return new ApiResponse<bool>(
                success,
                success ? "Company user deleted successfully" : "No changes were made",
                success
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting company user {CompanyUserId}: {Message}",
                request.CompanyUserId,
                ex.Message
            );
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
