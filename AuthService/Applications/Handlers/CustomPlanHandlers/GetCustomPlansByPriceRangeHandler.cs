using AuthService.DTOs.CustomModuleDTOs;
using AuthService.DTOs.CustomPlanDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomPlanQueries;

namespace AuthService.Handlers.CustomPlanHandlers;

/// Handler para obtener CustomPlans por rango de precios
public class GetCustomPlansByPriceRangeHandler
    : IRequestHandler<GetCustomPlansByPriceRangeQuery, ApiResponse<IEnumerable<CustomPlanDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCustomPlansByPriceRangeHandler> _logger;

    public GetCustomPlansByPriceRangeHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCustomPlansByPriceRangeHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<CustomPlanDTO>>> Handle(
        GetCustomPlansByPriceRangeQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (request.MinPrice < 0 || request.MaxPrice < 0 || request.MinPrice > request.MaxPrice)
            {
                return new ApiResponse<IEnumerable<CustomPlanDTO>>(
                    false,
                    "Invalid price range",
                    null!
                );
            }

            var customPlansQuery =
                from cp in _dbContext.CustomPlans
                join c in _dbContext.Companies on cp.CompanyId equals c.Id
                where cp.Price >= request.MinPrice && cp.Price <= request.MaxPrice
                orderby cp.Price
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
                "CustomPlans by price range retrieved successfully",
                customPlansDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CustomPlans by price range");
            return new ApiResponse<IEnumerable<CustomPlanDTO>>(
                false,
                "Error retrieving CustomPlans by price range",
                null!
            );
        }
    }
}
