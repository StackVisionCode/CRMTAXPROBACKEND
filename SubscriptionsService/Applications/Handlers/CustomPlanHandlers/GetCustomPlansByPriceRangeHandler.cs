using Common;
using DTOs.CustomModuleDTOs;
using DTOs.CustomPlanDTOs;
using Infrastructure.Context;
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
                    new List<CustomPlanDTO>()
                );
            }

            var customPlans = await _dbContext
                .CustomPlans.Where(cp =>
                    cp.Price >= request.MinPrice && cp.Price <= request.MaxPrice
                )
                .OrderBy(cp => cp.Price)
                .ToListAsync(cancellationToken);

            var customPlansDtos = customPlans
                .Select(cp => new CustomPlanDTO
                {
                    Id = cp.Id,
                    CompanyId = cp.CompanyId,
                    Price = cp.Price,
                    UserLimit = cp.UserLimit,
                    IsActive = cp.IsActive,
                    StartDate = cp.StartDate,
                    isRenewed = cp.IsRenewed,
                    RenewedDate = cp.RenewedDate,
                    RenewDate = cp.RenewDate,
                    AdditionalModuleNames = new List<string>(),
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
                new List<CustomPlanDTO>()
            );
        }
    }
}
