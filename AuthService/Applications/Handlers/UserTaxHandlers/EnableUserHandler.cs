using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserHandlers;

public class EnableUserHandler : IRequestHandler<EnableUserCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<EnableUserHandler> _logger;

    public EnableUserHandler(ApplicationDbContext dbContext, ILogger<EnableUserHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        EnableUserCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. Buscar TaxUser con información de company y plan
            var userQuery =
                from u in _dbContext.TaxUsers
                join c in _dbContext.Companies on u.CompanyId equals c.Id
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where u.Id == request.UserId
                select new
                {
                    User = u,
                    Company = c,
                    CustomPlan = cp,
                    UserLimit = cp.UserLimit,
                    CurrentActiveUserCount = _dbContext.TaxUsers.Count(tu =>
                        tu.CompanyId == u.CompanyId && tu.IsActive && tu.Id != u.Id
                    ),
                };

            var userData = await userQuery.FirstOrDefaultAsync(cancellationToken);
            if (userData?.User == null)
            {
                return new ApiResponse<bool>(false, "User not found", false);
            }

            var user = userData.User;

            if (user.IsActive)
            {
                return new ApiResponse<bool>(false, "User is already active", false);
            }

            // 2. Verificar límites del plan antes de habilitar
            if (userData.CurrentActiveUserCount >= userData.UserLimit)
            {
                _logger.LogWarning(
                    "Cannot enable user: User limit exceeded for company {CompanyId}. Current: {Current}, Limit: {Limit}",
                    userData.Company.Id,
                    userData.CurrentActiveUserCount,
                    userData.UserLimit
                );
                return new ApiResponse<bool>(
                    false,
                    $"Cannot enable user: Plan limit ({userData.UserLimit}) would be exceeded. Current active users: {userData.CurrentActiveUserCount}",
                    false
                );
            }

            // 3. Habilitar usuario
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            // 4. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                _logger.LogInformation("User enabled: {UserId}", request.UserId);
                return new ApiResponse<bool>(true, "User enabled successfully", true);
            }

            return new ApiResponse<bool>(false, "Failed to enable user", false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling user: {UserId}", request.UserId);
            return new ApiResponse<bool>(false, "Error enabling user", false);
        }
    }
}
