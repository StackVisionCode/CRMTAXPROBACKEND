using Common;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomModuleQueries;

namespace AuthService.Handlers.CustomModuleHandlers;

/// Handler para obtener CustomModules con estadísticas
public class GetCustomModulesWithStatsHandler
    : IRequestHandler<
        GetCustomModulesWithStatsQuery,
        ApiResponse<IEnumerable<CustomModuleWithStatsDTO>>
    >
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCustomModulesWithStatsHandler> _logger;

    public GetCustomModulesWithStatsHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCustomModulesWithStatsHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<CustomModuleWithStatsDTO>>> Handle(
        GetCustomModulesWithStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var now = DateTime.UtcNow;

            // CORREGIDO: Sin JOIN a Companies, solo datos disponibles
            var customModulesStatsQuery =
                from cm in _dbContext.CustomModules
                join cp in _dbContext.CustomPlans on cm.CustomPlanId equals cp.Id
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                select new
                {
                    CustomModule = cm,
                    Module = m,
                    CustomPlan = cp,
                };

            var statsData = await customModulesStatsQuery.ToListAsync(cancellationToken);

            var statsDto = statsData
                .Select(sd => new CustomModuleWithStatsDTO
                {
                    Id = sd.CustomModule.Id,
                    CustomPlanId = sd.CustomModule.CustomPlanId,
                    ModuleId = sd.CustomModule.ModuleId,
                    IsIncluded = sd.CustomModule.IsIncluded,
                    ModuleName = sd.Module.Name,
                    ModuleDescription = sd.Module.Description,
                    ModuleUrl = sd.Module.Url,

                    // Solo estadísticas disponibles en SubscriptionsService
                    CustomPlanIsActive = sd.CustomPlan.IsActive,
                    CustomPlanUserLimit = sd.CustomPlan.UserLimit,
                    CustomPlanPrice = sd.CustomPlan.Price,
                    DaysUntilPlanExpiry =
                        !sd.CustomPlan.IsRenewed && sd.CustomPlan.RenewDate > now
                            ? (int)(sd.CustomPlan.RenewDate - now).TotalDays
                            : (sd.CustomPlan.IsRenewed ? int.MaxValue : 0),

                    // Campos de Company se dejan vacíos - AuthService los llenará
                    CompanyName = null,
                    CompanyDomain = null,
                })
                .ToList();

            return new ApiResponse<IEnumerable<CustomModuleWithStatsDTO>>(
                true,
                "CustomModules with limited stats retrieved successfully",
                statsDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CustomModules with stats");
            return new ApiResponse<IEnumerable<CustomModuleWithStatsDTO>>(
                false,
                "Error retrieving CustomModules stats",
                new List<CustomModuleWithStatsDTO>()
            );
        }
    }
}
