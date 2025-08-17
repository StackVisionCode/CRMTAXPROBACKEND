using AuthService.DTOs.ModuleDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.ModuleQueries;

namespace AuthService.Handlers.ModuleHandlers;

public class GetAvailableModulesForCustomPlanHandler
    : IRequestHandler<GetAvailableModulesForCustomPlanQuery, ApiResponse<IEnumerable<ModuleDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetAvailableModulesForCustomPlanHandler> _logger;

    public GetAvailableModulesForCustomPlanHandler(
        ApplicationDbContext dbContext,
        ILogger<GetAvailableModulesForCustomPlanHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<ModuleDTO>>> Handle(
        GetAvailableModulesForCustomPlanQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Modules disponibles: Activos y sin Service (adicionales) o todos si queremos flexibilidad
            var modulesQuery =
                from m in _dbContext.Modules
                where m.IsActive
                orderby m.ServiceId == null descending, m.Name // Primero los adicionales
                select new
                {
                    Module = m,
                    ServiceName = m.ServiceId != null
                        ? (
                            from s in _dbContext.Services
                            where s.Id == m.ServiceId
                            select s.Name
                        ).FirstOrDefault()
                        : null,
                };

            var modulesData = await modulesQuery.ToListAsync(cancellationToken);

            var modulesDtos = modulesData
                .Select(md => new ModuleDTO
                {
                    Id = md.Module.Id,
                    Name = md.Module.Name,
                    Description = md.Module.Description,
                    Url = md.Module.Url,
                    IsActive = md.Module.IsActive,
                    ServiceId = md.Module.ServiceId,
                    ServiceName = md.ServiceName,
                })
                .ToList();

            return new ApiResponse<IEnumerable<ModuleDTO>>(
                true,
                "Available modules for CustomPlan retrieved successfully",
                modulesDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available Modules for CustomPlan");
            return new ApiResponse<IEnumerable<ModuleDTO>>(
                false,
                "Error retrieving available Modules",
                null!
            );
        }
    }
}
