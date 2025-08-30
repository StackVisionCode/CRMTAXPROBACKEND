using Common;
using DTOs.CustomModuleDTOs;
using DTOs.CustomPlanDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomPlanQueries;

namespace AuthService.Handlers.CustomPlanHandlers;

/// Handler para obtener CustomPlan por Company
public class GetCustomPlanByCompanyHandler
    : IRequestHandler<GetCustomPlanByCompanyQuery, ApiResponse<CustomPlanDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCustomPlanByCompanyHandler> _logger;

    public GetCustomPlanByCompanyHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCustomPlanByCompanyHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CustomPlanDTO>> Handle(
        GetCustomPlanByCompanyQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Query principal
            var customPlanQuery = await (
                from cp in _dbContext.CustomPlans
                where cp.CompanyId == request.CompanyId
                select new
                {
                    CustomPlan = cp,
                    // Obtener el primer Service de los módulos incluidos
                    BaseService = (
                        from cm in _dbContext.CustomModules
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        join s in _dbContext.Services on m.ServiceId equals s.Id
                        where cm.CustomPlanId == cp.Id && cm.IsIncluded
                        select s
                    ).FirstOrDefault(),
                    // Obtener módulos incluidos
                    CustomModules = (
                        from cm in _dbContext.CustomModules
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        where cm.CustomPlanId == cp.Id
                        select new CustomModuleDTO
                        {
                            Id = cm.Id,
                            CustomPlanId = cm.CustomPlanId,
                            ModuleId = cm.ModuleId,
                            IsIncluded = cm.IsIncluded,
                            ModuleName = m.Name,
                            ModuleDescription = m.Description,
                            ModuleUrl = m.Url,
                        }
                    ).ToList(),
                    // Obtener nombres de módulos incluidos
                    AdditionalModuleNames = (
                        from cm in _dbContext.CustomModules
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        where cm.CustomPlanId == cp.Id && cm.IsIncluded
                        select m.Name
                    ).ToList(),
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (customPlanQuery == null)
            {
                return new ApiResponse<CustomPlanDTO>(
                    true,
                    "No CustomPlan found for this Company",
                    null!
                );
            }

            var customPlanDto = new CustomPlanDTO
            {
                Id = customPlanQuery.CustomPlan.Id,
                CompanyId = customPlanQuery.CustomPlan.CompanyId,
                Price = customPlanQuery.CustomPlan.Price,
                UserLimit = customPlanQuery.CustomPlan.UserLimit,
                IsActive = customPlanQuery.CustomPlan.IsActive,
                StartDate = customPlanQuery.CustomPlan.StartDate,
                isRenewed = customPlanQuery.CustomPlan.IsRenewed,
                RenewedDate = customPlanQuery.CustomPlan.RenewedDate,
                RenewDate = customPlanQuery.CustomPlan.RenewDate,

                // Información del Service base
                BaseServiceId = customPlanQuery.BaseService?.Id,
                BaseServiceName = customPlanQuery.BaseService?.Name ?? "Custom",
                BaseServiceTitle = customPlanQuery.BaseService?.Title ?? "Custom Plan",
                BaseServiceLevel = (int?)customPlanQuery.BaseService?.ServiceLevel ?? 1,

                CustomModules = customPlanQuery.CustomModules,
                AdditionalModuleNames = customPlanQuery.AdditionalModuleNames,
            };

            return new ApiResponse<CustomPlanDTO>(
                true,
                "CustomPlan retrieved successfully",
                customPlanDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting CustomPlan by Company: {CompanyId}",
                request.CompanyId
            );
            return new ApiResponse<CustomPlanDTO>(false, "Error retrieving CustomPlan", null!);
        }
    }
}
