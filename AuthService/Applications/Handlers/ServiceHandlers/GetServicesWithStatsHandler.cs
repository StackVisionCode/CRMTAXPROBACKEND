using AuthService.DTOs.ServiceDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.ServiceQueries;

namespace AuthService.Handlers.ServiceHandlers;

public class GetServicesWithStatsHandler
    : IRequestHandler<GetServicesWithStatsQuery, ApiResponse<IEnumerable<ServiceWithStatsDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetServicesWithStatsHandler> _logger;

    public GetServicesWithStatsHandler(
        ApplicationDbContext dbContext,
        ILogger<GetServicesWithStatsHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<ServiceWithStatsDTO>>> Handle(
        GetServicesWithStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var servicesStatsQuery =
                from s in _dbContext.Services
                select new
                {
                    Service = s,
                    ModuleNames = (
                        from m in _dbContext.Modules
                        where m.ServiceId == s.Id && m.IsActive
                        select m.Name
                    ).ToList(),
                    ModuleIds = (
                        from m in _dbContext.Modules
                        where m.ServiceId == s.Id && m.IsActive
                        select m.Id
                    ).ToList(),
                    // Estadísticas con TaxUsers
                    CompaniesUsing = (
                        from c in _dbContext.Companies
                        join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                        join cm in _dbContext.CustomModules on cp.Id equals cm.CustomPlanId
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        where m.ServiceId == s.Id && cm.IsIncluded && cp.IsActive
                        select c.Id
                    )
                        .Distinct()
                        .Count(),
                    // Total de TaxUsers activos usando este servicio
                    TotalActiveTaxUsers = (
                        from c in _dbContext.Companies
                        join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                        join cm in _dbContext.CustomModules on cp.Id equals cm.CustomPlanId
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        join u in _dbContext.TaxUsers on c.Id equals u.CompanyId
                        where m.ServiceId == s.Id && cm.IsIncluded && cp.IsActive && u.IsActive
                        select u.Id
                    ).Count(),
                    // Conteo de Owners usando este servicio
                    TotalOwnersUsing = (
                        from c in _dbContext.Companies
                        join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                        join cm in _dbContext.CustomModules on cp.Id equals cm.CustomPlanId
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        join u in _dbContext.TaxUsers on c.Id equals u.CompanyId
                        where
                            m.ServiceId == s.Id
                            && cm.IsIncluded
                            && cp.IsActive
                            && u.IsOwner
                            && u.IsActive
                        select u.Id
                    ).Count(),
                    // Conteo de usuarios regulares usando este servicio
                    TotalRegularUsersUsing = (
                        from c in _dbContext.Companies
                        join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                        join cm in _dbContext.CustomModules on cp.Id equals cm.CustomPlanId
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        join u in _dbContext.TaxUsers on c.Id equals u.CompanyId
                        where
                            m.ServiceId == s.Id
                            && cm.IsIncluded
                            && cp.IsActive
                            && !u.IsOwner
                            && u.IsActive
                        select u.Id
                    ).Count(),
                    // Revenue total basado en CustomPlans activos
                    TotalRevenue = (
                        from c in _dbContext.Companies
                        join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                        join cm in _dbContext.CustomModules on cp.Id equals cm.CustomPlanId
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        where m.ServiceId == s.Id && cm.IsIncluded && cp.IsActive
                        select cp.Price
                    )
                        .DefaultIfEmpty(0m)
                        .Sum(),

                    AverageRevenuePerCompany = (
                        from c in _dbContext.Companies
                        join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                        join cm in _dbContext.CustomModules on cp.Id equals cm.CustomPlanId
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        where m.ServiceId == s.Id && cm.IsIncluded && cp.IsActive
                        select cp.Price
                    )
                        .DefaultIfEmpty(0m)
                        .Average(),
                };

            var statsData = await servicesStatsQuery.ToListAsync(cancellationToken);

            var statsDto = statsData
                .Select(sd => new ServiceWithStatsDTO
                {
                    Id = sd.Service.Id,
                    Name = sd.Service.Name,
                    Title = sd.Service.Title,
                    Description = sd.Service.Description,
                    Features = sd.Service.Features,
                    Price = sd.Service.Price,
                    UserLimit = sd.Service.UserLimit,
                    IsActive = sd.Service.IsActive,
                    ModuleNames = sd.ModuleNames,
                    ModuleIds = sd.ModuleIds,
                    CreatedAt = sd.Service.CreatedAt,

                    // Estadísticas actualizadas
                    CompaniesUsingCount = sd.CompaniesUsing,
                    TotalActiveUsers = sd.TotalActiveTaxUsers,
                    TotalOwnersUsing = sd.TotalOwnersUsing,
                    TotalRegularUsersUsing = sd.TotalRegularUsersUsing,
                    TotalRevenue = sd.TotalRevenue,
                    AverageRevenuePerCompany = sd.AverageRevenuePerCompany,

                    // Ratios y métricas calculadas
                    AverageUsersPerCompany =
                        sd.CompaniesUsing > 0
                            ? (double)sd.TotalActiveTaxUsers / sd.CompaniesUsing
                            : 0,
                    RevenuePerUser =
                        sd.TotalActiveTaxUsers > 0 ? sd.TotalRevenue / sd.TotalActiveTaxUsers : 0,
                })
                .OrderBy(s => s.Name) // Orden alfabético
                .ToList();

            _logger.LogInformation(
                "Retrieved {Count} services with stats. Total companies using services: {TotalCompanies}",
                statsDto.Count,
                statsDto.Sum(s => s.CompaniesUsingCount)
            );

            return new ApiResponse<IEnumerable<ServiceWithStatsDTO>>(
                true,
                "Services with stats retrieved successfully",
                statsDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Services with stats: {Message}", ex.Message);
            return new ApiResponse<IEnumerable<ServiceWithStatsDTO>>(
                false,
                "Error retrieving Services stats",
                new List<ServiceWithStatsDTO>()
            );
        }
    }
}
