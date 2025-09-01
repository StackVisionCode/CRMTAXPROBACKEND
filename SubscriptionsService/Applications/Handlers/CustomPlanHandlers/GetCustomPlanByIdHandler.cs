using Common;
using DTOs.CustomModuleDTOs;
using DTOs.CustomPlanDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomPlanQueries;

namespace AuthService.Handlers.CustomPlanHandlers;

/// Handler para obtener CustomPlan por ID
public class GetCustomPlanByIdHandler
    : IRequestHandler<GetCustomPlanByIdQuery, ApiResponse<CustomPlanDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCustomPlanByIdHandler> _logger;

    public GetCustomPlanByIdHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCustomPlanByIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CustomPlanDTO>> Handle(
        GetCustomPlanByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var customPlanQuery =
                from cp in _dbContext.CustomPlans
                where cp.Id == request.CustomPlanId
                select new
                {
                    CustomPlan = cp,
                    CustomModules = (
                        from cm in _dbContext.CustomModules
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        where cm.CustomPlanId == cp.Id
                        select new { CustomModule = cm, Module = m }
                    ).ToList(),
                };

            var planData = await customPlanQuery.FirstOrDefaultAsync(cancellationToken);
            if (planData?.CustomPlan == null)
            {
                _logger.LogWarning("CustomPlan not found: {CustomPlanId}", request.CustomPlanId);
                return new ApiResponse<CustomPlanDTO>(false, "CustomPlan not found", null!);
            }

            var customPlanDto = new CustomPlanDTO
            {
                Id = planData.CustomPlan.Id,
                CompanyId = planData.CustomPlan.CompanyId,
                Price = planData.CustomPlan.Price,
                UserLimit = planData.CustomPlan.UserLimit,
                IsActive = planData.CustomPlan.IsActive,
                StartDate = planData.CustomPlan.StartDate,
                isRenewed = planData.CustomPlan.IsRenewed,
                RenewedDate = planData.CustomPlan.RenewedDate,
                RenewDate = planData.CustomPlan.RenewDate,
                CustomModules = planData
                    .CustomModules.Select(cm => new CustomModuleDTO
                    {
                        Id = cm.CustomModule.Id,
                        CustomPlanId = cm.CustomModule.CustomPlanId,
                        ModuleId = cm.CustomModule.ModuleId,
                        IsIncluded = cm.CustomModule.IsIncluded,
                        ModuleName = cm.Module.Name,
                        ModuleDescription = cm.Module.Description,
                        ModuleUrl = cm.Module.Url,
                    })
                    .ToList(),
                AdditionalModuleNames = planData
                    .CustomModules.Where(cm => cm.CustomModule.IsIncluded)
                    .Select(cm => cm.Module.Name)
                    .ToList(),
            };

            return new ApiResponse<CustomPlanDTO>(
                true,
                "CustomPlan retrieved successfully",
                customPlanDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting CustomPlan: {CustomPlanId}", request.CustomPlanId);
            return new ApiResponse<CustomPlanDTO>(false, "Error retrieving CustomPlan", null!);
        }
    }
}
