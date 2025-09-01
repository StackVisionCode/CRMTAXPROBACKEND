using Common;
using DTOs.CustomModuleDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomModuleQueries;

namespace AuthService.Handlers.CustomModuleHandlers;

/// Handler para obtener CustomModules activos por Company
public class GetActiveCustomModulesByCompanyHandler
    : IRequestHandler<
        GetActiveCustomModulesByCompanyQuery,
        ApiResponse<IEnumerable<CustomModuleDTO>>
    >
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetActiveCustomModulesByCompanyHandler> _logger;

    public GetActiveCustomModulesByCompanyHandler(
        ApplicationDbContext dbContext,
        ILogger<GetActiveCustomModulesByCompanyHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<CustomModuleDTO>>> Handle(
        GetActiveCustomModulesByCompanyQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // CORREGIDO: Sin JOIN a Companies, solo usar CompanyId
            var customModulesQuery =
                from cm in _dbContext.CustomModules
                join cp in _dbContext.CustomPlans on cm.CustomPlanId equals cp.Id
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                where
                    cp.CompanyId == request.CompanyId && cm.IsIncluded && cp.IsActive && m.IsActive
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
                "Active CustomModules by company retrieved successfully",
                customModulesDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting active CustomModules by company: {CompanyId}",
                request.CompanyId
            );
            return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                false,
                "Error retrieving active CustomModules by company",
                new List<CustomModuleDTO>()
            );
        }
    }
}
