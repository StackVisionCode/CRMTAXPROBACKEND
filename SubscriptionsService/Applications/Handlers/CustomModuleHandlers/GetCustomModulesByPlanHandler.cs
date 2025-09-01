using Common;
using DTOs.CustomModuleDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomModuleQueries;

namespace AuthService.Handlers.CustomModuleHandlers;

/// Handler para obtener CustomModules por CustomPlan
public class GetCustomModulesByPlanHandler
    : IRequestHandler<GetCustomModulesByPlanQuery, ApiResponse<IEnumerable<CustomModuleDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCustomModulesByPlanHandler> _logger;

    public GetCustomModulesByPlanHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCustomModulesByPlanHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<CustomModuleDTO>>> Handle(
        GetCustomModulesByPlanQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Verificar que el CustomPlan existe
            var customPlanExists = await _dbContext.CustomPlans.AnyAsync(
                cp => cp.Id == request.CustomPlanId,
                cancellationToken
            );

            if (!customPlanExists)
            {
                return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                    false,
                    "CustomPlan not found",
                    null!
                );
            }

            var customModulesQuery =
                from cm in _dbContext.CustomModules
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                where
                    cm.CustomPlanId == request.CustomPlanId
                    && (request.IsIncluded == null || cm.IsIncluded == request.IsIncluded)
                orderby m.Name
                select new { CustomModule = cm, Module = m };

            var modulesData = await customModulesQuery.ToListAsync(cancellationToken);

            var customModulesDtos = modulesData
                .Select(md => new CustomModuleDTO
                {
                    Id = md.CustomModule.Id,
                    CustomPlanId = md.CustomModule.CustomPlanId,
                    ModuleId = md.CustomModule.ModuleId,
                    IsIncluded = md.CustomModule.IsIncluded,
                    ModuleName = md.Module.Name,
                    ModuleDescription = md.Module.Description,
                    ModuleUrl = md.Module.Url,
                })
                .ToList();

            return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                true,
                "CustomModules by plan retrieved successfully",
                customModulesDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting CustomModules by plan: {CustomPlanId}",
                request.CustomPlanId
            );
            return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                false,
                "Error retrieving CustomModules by plan",
                null!
            );
        }
    }
}
