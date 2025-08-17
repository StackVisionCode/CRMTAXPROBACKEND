using AuthService.DTOs.CustomModuleDTOs;
using AuthService.DTOs.CustomPlanDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomPlanQueries;

namespace AuthService.Handlers.CustomPlanHandlers;

/// Handler para obtener CustomPlans que expiran pronto
public class GetExpiringCustomPlansHandler
    : IRequestHandler<GetExpiringCustomPlansQuery, ApiResponse<IEnumerable<CustomPlanDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetExpiringCustomPlansHandler> _logger;

    public GetExpiringCustomPlansHandler(
        ApplicationDbContext dbContext,
        ILogger<GetExpiringCustomPlansHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<CustomPlanDTO>>> Handle(
        GetExpiringCustomPlansQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var now = DateTime.UtcNow;
            var futureDate = now.AddDays(request.DaysAhead);

            var expiringPlansQuery =
                from cp in _dbContext.CustomPlans
                join c in _dbContext.Companies on cp.CompanyId equals c.Id
                where
                    cp.IsActive
                    && !cp.isRenewed
                    && cp.RenewDate >= now
                    && cp.RenewDate <= futureDate
                orderby cp.RenewDate
                select new
                {
                    CustomPlan = cp,
                    Company = c,
                    ModuleNames = (
                        from cm in _dbContext.CustomModules
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        where cm.CustomPlanId == cp.Id && cm.IsIncluded
                        select m.Name
                    ).ToList(),
                };

            var plansData = await expiringPlansQuery.ToListAsync(cancellationToken);

            var customPlansDtos = plansData
                .Select(pd => new CustomPlanDTO
                {
                    Id = pd.CustomPlan.Id,
                    CompanyId = pd.CustomPlan.CompanyId,
                    Price = pd.CustomPlan.Price,
                    UserLimit = pd.CustomPlan.UserLimit,
                    IsActive = pd.CustomPlan.IsActive,
                    StartDate = pd.CustomPlan.StartDate,
                    isRenewed = pd.CustomPlan.isRenewed,
                    RenewedDate = pd.CustomPlan.RenewedDate,
                    RenewDate = pd.CustomPlan.RenewDate,
                    CompanyName = pd.Company.IsCompany
                        ? pd.Company.CompanyName
                        : pd.Company.FullName,
                    CompanyDomain = pd.Company.Domain,
                    AdditionalModuleNames = pd.ModuleNames,
                    CustomModules = new List<CustomModuleDTO>(),
                })
                .ToList();

            _logger.LogInformation(
                "Found {Count} CustomPlans expiring within {Days} days",
                customPlansDtos.Count,
                request.DaysAhead
            );

            return new ApiResponse<IEnumerable<CustomPlanDTO>>(
                true,
                "Expiring CustomPlans retrieved successfully",
                customPlansDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expiring CustomPlans");
            return new ApiResponse<IEnumerable<CustomPlanDTO>>(
                false,
                "Error retrieving expiring CustomPlans",
                null!
            );
        }
    }
}
