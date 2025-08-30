using Common;
using DTOs.CustomModuleDTOs;
using DTOs.CustomPlanDTOs;
using Infrastructure.Context;
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

            var customPlansQuery = _dbContext
                .CustomPlans.Where(cp =>
                    (request.IsActive == null || cp.IsActive == request.IsActive)
                    && (
                        request.IsExpired == null
                        || (
                            request.IsExpired == true
                                ? cp.RenewDate < now && !cp.IsRenewed
                                : (cp.RenewDate >= now || cp.IsRenewed)
                        )
                    )
                )
                .OrderByDescending(cp => cp.CreatedAt);

            var customPlans = await customPlansQuery.ToListAsync(cancellationToken);

            var customPlansDtos = customPlans
                .Select(cp => new CustomPlanDTO
                {
                    Id = cp.Id,
                    CompanyId = cp.CompanyId, // Solo el ID
                    Price = cp.Price,
                    UserLimit = cp.UserLimit,
                    IsActive = cp.IsActive,
                    StartDate = cp.StartDate,
                    isRenewed = cp.IsRenewed,
                    RenewedDate = cp.RenewedDate,
                    RenewDate = cp.RenewDate,
                    AdditionalModuleNames = new List<string>(), // Se puede llenar con una query separada si se necesita
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
                new List<CustomPlanDTO>()
            );
        }
    }
}
