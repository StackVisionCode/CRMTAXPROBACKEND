using AuthService.DTOs.CustomModuleDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomModuleQueries;

namespace AuthService.Handlers.CustomModuleHandlers;

public class GetAllCustomModulesHandler
    : IRequestHandler<GetAllCustomModulesQuery, ApiResponse<IEnumerable<CustomModuleDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetAllCustomModulesHandler> _logger;

    public GetAllCustomModulesHandler(
        ApplicationDbContext dbContext,
        ILogger<GetAllCustomModulesHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<CustomModuleDTO>>> Handle(
        GetAllCustomModulesQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var customModulesQuery =
                from cm in _dbContext.CustomModules
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                where
                    (request.IsIncluded == null || cm.IsIncluded == request.IsIncluded)
                    && (request.CustomPlanId == null || cm.CustomPlanId == request.CustomPlanId)
                orderby cm.CreatedAt descending
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
                "CustomModules retrieved successfully",
                customModulesDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all CustomModules");
            return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                false,
                "Error retrieving CustomModules",
                null!
            );
        }
    }
}
