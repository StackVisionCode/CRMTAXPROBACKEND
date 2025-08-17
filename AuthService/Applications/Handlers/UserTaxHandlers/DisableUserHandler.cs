using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserHandlers;

public class DisableUserHandler : IRequestHandler<DisableUserCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DisableUserHandler> _logger;

    public DisableUserHandler(ApplicationDbContext dbContext, ILogger<DisableUserHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DisableUserCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. Buscar TaxUser
            var userQuery =
                from u in _dbContext.TaxUsers
                join c in _dbContext.Companies on u.CompanyId equals c.Id
                where u.Id == request.UserId
                select new { User = u, Company = c };

            var userData = await userQuery.FirstOrDefaultAsync(cancellationToken);
            if (userData?.User == null)
            {
                return new ApiResponse<bool>(false, "User not found", false);
            }

            var user = userData.User;

            if (!user.IsActive)
            {
                return new ApiResponse<bool>(false, "User is already disabled", false);
            }

            // 2. Verificar si es Owner - no se puede deshabilitar el último Owner
            if (user.IsOwner)
            {
                var otherOwnersCount = await _dbContext
                    .TaxUsers.Where(u =>
                        u.CompanyId == user.CompanyId && u.IsOwner && u.IsActive && u.Id != user.Id
                    )
                    .CountAsync(cancellationToken);

                if (otherOwnersCount == 0)
                {
                    return new ApiResponse<bool>(
                        false,
                        "Cannot disable the last active owner of the company",
                        false
                    );
                }
            }

            // 3. Deshabilitar usuario
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            // 4. Revocar todas las sesiones activas
            var activeSessions = await _dbContext
                .Sessions.Where(s => s.TaxUserId == request.UserId && !s.IsRevoke)
                .ToListAsync(cancellationToken);

            foreach (var session in activeSessions)
            {
                session.IsRevoke = true;
                session.UpdatedAt = DateTime.UtcNow;
            }

            // 5. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                _logger.LogInformation("User disabled: {UserId}", request.UserId);
                return new ApiResponse<bool>(true, "User disabled successfully", true);
            }

            return new ApiResponse<bool>(false, "Failed to disable user", false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disabling user: {UserId}", request.UserId);
            return new ApiResponse<bool>(false, "Error disabling user", false);
        }
    }
}
