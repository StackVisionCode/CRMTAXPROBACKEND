using Common;
using DTOs.CustomModuleDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomModuleQueries;

namespace AuthService.Handlers.CustomModuleHandlers;

/// Handler para obtener CustomModules por Module
public class GetCustomModulesByModuleHandler
    : IRequestHandler<GetCustomModulesByModuleQuery, ApiResponse<IEnumerable<CustomModuleDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCustomModulesByModuleHandler> _logger;

    public GetCustomModulesByModuleHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCustomModulesByModuleHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<CustomModuleDTO>>> Handle(
        GetCustomModulesByModuleQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Verificar que el Module existe
            var moduleExists = await _dbContext.Modules.AnyAsync(
                m => m.Id == request.ModuleId,
                cancellationToken
            );

            if (!moduleExists)
            {
                return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                    false,
                    "Module not found",
                    null!
                );
            }

            var customModulesQuery =
                from cm in _dbContext.CustomModules
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                where
                    cm.ModuleId == request.ModuleId
                    && (request.IsIncluded == null || cm.IsIncluded == request.IsIncluded)
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
                "CustomModules by module retrieved successfully",
                customModulesDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting CustomModules by module: {ModuleId}",
                request.ModuleId
            );
            return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                false,
                "Error retrieving CustomModules by module",
                null!
            );
        }
    }
}
