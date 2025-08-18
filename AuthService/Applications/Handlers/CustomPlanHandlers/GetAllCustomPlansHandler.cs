using AuthService.DTOs.CustomModuleDTOs;
using AuthService.DTOs.CustomPlanDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomPlanQueries;

namespace AuthService.Handlers.CustomPlanHandlers;

/// Handler para obtener todos los CustomPlans
public class GetAllCustomPlansHandler
    : IRequestHandler<GetAllCustomPlansQuery, ApiResponse<IEnumerable<CustomPlanDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetAllCustomPlansHandler> _logger;

    public GetAllCustomPlansHandler(
        ApplicationDbContext dbContext,
        ILogger<GetAllCustomPlansHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<CustomPlanDTO>>> Handle(
        GetAllCustomPlansQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var now = DateTime.UtcNow;

            var customPlansQuery =
                from cp in _dbContext.CustomPlans
                join c in _dbContext.Companies on cp.CompanyId equals c.Id
                where
                    (request.IsActive == null || cp.IsActive == request.IsActive)
                    && (
                        request.IsExpired == null
                        || (
                            request.IsExpired == true
                                ? cp.RenewDate < now && !cp.isRenewed
                                : (cp.RenewDate >= now || cp.isRenewed)
                        )
                    )
                orderby cp.CreatedAt descending
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

            var plansData = await customPlansQuery.ToListAsync(cancellationToken);

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

            return new ApiResponse<IEnumerable<CustomPlanDTO>>(
                true,
                "CustomPlans retrieved successfully",
                customPlansDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all CustomPlans");
            return new ApiResponse<IEnumerable<CustomPlanDTO>>(
                false,
                "Error retrieving CustomPlans",
                null!
            );
        }
    }
}
