using Common;
using DTOs.CustomModuleDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomModuleQueries;

namespace AuthService.Handlers.CustomModuleHandlers;

/// Handler para obtener CustomModule por ID
public class GetCustomModuleByIdHandler
    : IRequestHandler<GetCustomModuleByIdQuery, ApiResponse<CustomModuleDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetCustomModuleByIdHandler> _logger;

    public GetCustomModuleByIdHandler(
        ApplicationDbContext dbContext,
        ILogger<GetCustomModuleByIdHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CustomModuleDTO>> Handle(
        GetCustomModuleByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var customModuleQuery =
                from cm in _dbContext.CustomModules
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                where cm.Id == request.CustomModuleId
                select new { CustomModule = cm, Module = m };

            var moduleData = await customModuleQuery.FirstOrDefaultAsync(cancellationToken);
            if (moduleData?.CustomModule == null)
            {
                _logger.LogWarning(
                    "CustomModule not found: {CustomModuleId}",
                    request.CustomModuleId
                );
                return new ApiResponse<CustomModuleDTO>(false, "CustomModule not found", null!);
            }

            var customModuleDto = new CustomModuleDTO
            {
                Id = moduleData.CustomModule.Id,
                CustomPlanId = moduleData.CustomModule.CustomPlanId,
                ModuleId = moduleData.CustomModule.ModuleId,
                IsIncluded = moduleData.CustomModule.IsIncluded,
                ModuleName = moduleData.Module.Name,
                ModuleDescription = moduleData.Module.Description,
                ModuleUrl = moduleData.Module.Url,
            };

            return new ApiResponse<CustomModuleDTO>(
                true,
                "CustomModule retrieved successfully",
                customModuleDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting CustomModule: {CustomModuleId}",
                request.CustomModuleId
            );
            return new ApiResponse<CustomModuleDTO>(false, "Error retrieving CustomModule", null!);
        }
    }
}
