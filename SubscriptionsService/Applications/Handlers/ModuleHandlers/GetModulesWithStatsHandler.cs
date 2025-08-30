using Common;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.ModuleQueries;

namespace AuthService.Handlers.ModuleHandlers;

public class GetModulesWithStatsHandler
    : IRequestHandler<GetModulesWithStatsQuery, ApiResponse<IEnumerable<ModuleWithStatsDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetModulesWithStatsHandler> _logger;

    public GetModulesWithStatsHandler(
        ApplicationDbContext dbContext,
        ILogger<GetModulesWithStatsHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<ModuleWithStatsDTO>>> Handle(
        GetModulesWithStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var modulesStatsQuery =
                from m in _dbContext.Modules
                select new
                {
                    Module = m,
                    ServiceName = m.ServiceId != null
                        ? (
                            from s in _dbContext.Services
                            where s.Id == m.ServiceId
                            select s.Name
                        ).FirstOrDefault()
                        : null,
                    // Estadísticas
                    CustomPlansUsing = (
                        from cm in _dbContext.CustomModules
                        where cm.ModuleId == m.Id && cm.IsIncluded
                        select cm.CustomPlanId
                    )
                        .Distinct()
                        .Count(),
                    CompaniesUsing = (
                        from cm in _dbContext.CustomModules
                        join cp in _dbContext.CustomPlans on cm.CustomPlanId equals cp.Id
                        where cm.ModuleId == m.Id && cm.IsIncluded && cp.IsActive
                        select cp.CompanyId
                    )
                        .Distinct()
                        .Count(),
                    IsBaseModule = m.ServiceId != null,
                    IsAdditionalModule = (
                        from cm in _dbContext.CustomModules
                        where cm.ModuleId == m.Id && cm.IsIncluded
                        select cm.Id
                    ).Any(),
                };

            var statsData = await modulesStatsQuery.ToListAsync(cancellationToken);

            var statsDto = statsData
                .Select(sd => new ModuleWithStatsDTO
                {
                    Id = sd.Module.Id,
                    Name = sd.Module.Name,
                    Description = sd.Module.Description,
                    Url = sd.Module.Url,
                    IsActive = sd.Module.IsActive,
                    ServiceId = sd.Module.ServiceId,
                    ServiceName = sd.ServiceName,
                    // Estadísticas
                    CustomPlansUsingCount = sd.CustomPlansUsing,
                    CompaniesUsingCount = sd.CompaniesUsing,
                    IsBaseModule = sd.IsBaseModule,
                    IsAdditionalModule = sd.IsAdditionalModule,
                })
                .ToList();

            return new ApiResponse<IEnumerable<ModuleWithStatsDTO>>(
                true,
                "Modules with stats retrieved successfully",
                statsDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Modules with stats");
            return new ApiResponse<IEnumerable<ModuleWithStatsDTO>>(
                false,
                "Error retrieving Modules stats",
                null!
            );
        }
    }
}
