using Common;
using DTOs.CustomModuleDTOs;
using DTOs.CustomPlanDTOs;
using Infrastructure.Context;
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

            var expiringPlans = await _dbContext
                .CustomPlans.Where(cp =>
                    cp.IsActive
                    && !cp.IsRenewed
                    && cp.RenewDate >= now
                    && cp.RenewDate <= futureDate
                )
                .OrderBy(cp => cp.RenewDate)
                .ToListAsync(cancellationToken);

            var customPlansDtos = expiringPlans
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
                new List<CustomPlanDTO>()
            );
        }
    }
}
