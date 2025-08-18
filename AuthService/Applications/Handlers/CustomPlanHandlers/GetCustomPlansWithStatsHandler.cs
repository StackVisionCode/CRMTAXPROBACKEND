using AuthService.DTOs.CustomModuleDTOs;
using AuthService.DTOs.CustomPlanDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomPlanQueries;

namespace AuthService.Handlers.CustomPlanHandlers;

/// Handler para obtener CustomPlans con estadísticas
public class GetCustomPlansWithStatsHandler
    : IRequestHandler<
        GetCustomPlansWithStatsQuery,
        ApiResponse<IEnumerable<CustomPlanWithStatsDTO>>
    >
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCustomPlansWithStatsHandler> _logger;

    public GetCustomPlansWithStatsHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCustomPlansWithStatsHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<CustomPlanWithStatsDTO>>> Handle(
        GetCustomPlansWithStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var now = DateTime.UtcNow;

            var plansStatsQuery =
                from cp in _dbContext.CustomPlans
                join c in _dbContext.Companies on cp.CompanyId equals c.Id
                select new
                {
                    CustomPlan = cp,
                    Company = c,
                    // Estadísticas de TaxUsers en lugar de UserCompanies
                    TotalTaxUsers = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id),
                    ActiveTaxUsers = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsActive
                    ),
                    OwnerCount = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id && u.IsOwner),
                    RegularUsersCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && !u.IsOwner
                    ),

                    // Estadísticas de módulos (sin cambios)
                    TotalModulesCount = (
                        from cm in _dbContext.CustomModules
                        where cm.CustomPlanId == cp.Id
                        select cm.Id
                    ).Count(),
                    ActiveModulesCount = (
                        from cm in _dbContext.CustomModules
                        where cm.CustomPlanId == cp.Id && cm.IsIncluded
                        select cm.Id
                    ).Count(),

                    // Nombres de módulos (sin cambios)
                    ModuleNames = (
                        from cm in _dbContext.CustomModules
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        where cm.CustomPlanId == cp.Id && cm.IsIncluded
                        select m.Name
                    ).ToList(),

                    // Información adicional para estadísticas
                    BaseModuleNames = (
                        from cm in _dbContext.CustomModules
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        where cm.CustomPlanId == cp.Id && cm.IsIncluded && m.ServiceId != null
                        select m.Name
                    ).ToList(),
                    AdditionalModuleNames = (
                        from cm in _dbContext.CustomModules
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        where cm.CustomPlanId == cp.Id && cm.IsIncluded && m.ServiceId == null
                        select m.Name
                    ).ToList(),

                    // Información del servicio base
                    BaseServiceName = (
                        from cm in _dbContext.CustomModules
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        join s in _dbContext.Services on m.ServiceId equals s.Id
                        where cm.CustomPlanId == cp.Id && cm.IsIncluded && m.ServiceId != null
                        select s.Name
                    ).FirstOrDefault(),

                    // Límite de usuarios del servicio
                    ServiceUserLimit = (
                        from cm in _dbContext.CustomModules
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        join s in _dbContext.Services on m.ServiceId equals s.Id
                        where cm.CustomPlanId == cp.Id && cm.IsIncluded && m.ServiceId != null
                        select s.UserLimit
                    ).FirstOrDefault(),
                };

            var statsData = await plansStatsQuery.ToListAsync(cancellationToken);

            var statsDto = statsData
                .Select(sd => new CustomPlanWithStatsDTO
                {
                    Id = sd.CustomPlan.Id,
                    CompanyId = sd.CustomPlan.CompanyId,
                    Price = sd.CustomPlan.Price,
                    UserLimit = sd.CustomPlan.UserLimit,
                    IsActive = sd.CustomPlan.IsActive,
                    StartDate = sd.CustomPlan.StartDate,
                    isRenewed = sd.CustomPlan.isRenewed,
                    RenewedDate = sd.CustomPlan.RenewedDate,
                    RenewDate = sd.CustomPlan.RenewDate,
                    CompanyName = sd.Company.IsCompany
                        ? sd.Company.CompanyName
                        : sd.Company.FullName,
                    CompanyDomain = sd.Company.Domain,
                    AdditionalModuleNames = sd.ModuleNames,
                    CustomModules = new List<CustomModuleDTO>(),

                    // Estadísticas actualizadas
                    TotalModules = sd.TotalModulesCount,
                    ActiveModules = sd.ActiveModulesCount,
                    TotalUsers = sd.TotalTaxUsers,
                    ActiveUsers = sd.ActiveTaxUsers,
                    OwnerCount = sd.OwnerCount,
                    RegularUsersCount = sd.RegularUsersCount,

                    // Información del servicio
                    BaseServiceName = sd.BaseServiceName,
                    ServiceUserLimit = sd.ServiceUserLimit,
                    // 🔧 CAMBIO CRÍTICO: Comparar contra CustomPlan.UserLimit, no ServiceUserLimit
                    IsWithinLimits = sd.ActiveTaxUsers <= sd.CustomPlan.UserLimit,

                    // 🔧 CAMBIO: Usar RenewDate en lugar de EndDate
                    IsExpired = !sd.CustomPlan.isRenewed && sd.CustomPlan.RenewDate < now,
                    DaysUntilExpiry =
                        !sd.CustomPlan.isRenewed && sd.CustomPlan.RenewDate > now
                            ? Math.Max(0, (int)(sd.CustomPlan.RenewDate - now).TotalDays)
                            : (sd.CustomPlan.isRenewed ? int.MaxValue : 0),
                    MonthlyRevenue = sd.CustomPlan.Price, // Asumiendo precio mensual

                    // Métricas adicionales
                    RevenuePerUser =
                        sd.ActiveTaxUsers > 0 ? sd.CustomPlan.Price / sd.ActiveTaxUsers : 0,
                    ModuleUtilization =
                        sd.TotalModulesCount > 0
                            ? (double)sd.ActiveModulesCount / sd.TotalModulesCount * 100
                            : 0,

                    // Separación de módulos
                    BaseModuleNames = sd.BaseModuleNames,
                    ExtraModuleNames = sd.AdditionalModuleNames,
                })
                .OrderBy(p => p.CompanyName) // Orden alfabético por company
                .ToList();

            _logger.LogInformation(
                "Retrieved {Count} CustomPlans with stats. Total active users: {TotalUsers}",
                statsDto.Count,
                statsDto.Sum(p => p.ActiveUsers)
            );

            return new ApiResponse<IEnumerable<CustomPlanWithStatsDTO>>(
                true,
                "CustomPlans with stats retrieved successfully",
                statsDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CustomPlans with stats: {Message}", ex.Message);
            return new ApiResponse<IEnumerable<CustomPlanWithStatsDTO>>(
                false,
                "Error retrieving CustomPlans stats",
                new List<CustomPlanWithStatsDTO>()
            );
        }
    }
}
