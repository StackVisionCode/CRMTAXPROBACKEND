using Common;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomModuleQueries;

namespace AuthService.Handlers.CustomModuleHandlers;

/// Handler para obtener módulos disponibles para un CustomPlan
public class GetAvailableModulesForCustomPlanHandler
    : IRequestHandler<
        GetAvailableModulesForCustomPlanQuery,
        ApiResponse<IEnumerable<ModuleAvailabilityDTO>>
    >
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

    public async Task<ApiResponse<IEnumerable<ModuleAvailabilityDTO>>> Handle(
        GetAvailableModulesForCustomPlanQuery request,
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
                return new ApiResponse<IEnumerable<ModuleAvailabilityDTO>>(
                    false,
                    "CustomPlan not found",
                    null!
                );
            }

            // Obtener módulos ya asignados al plan
            var assignedModuleIds = await _dbContext
                .CustomModules.Where(cm => cm.CustomPlanId == request.CustomPlanId)
                .Select(cm => cm.ModuleId)
                .ToListAsync(cancellationToken);

            // Obtener todos los módulos activos con información de servicio
            var modulesQuery =
                from m in _dbContext.Modules
                where m.IsActive
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

            var availabilityDtos = modulesData
                .Select(md => new ModuleAvailabilityDTO
                {
                    ModuleId = md.Module.Id,
                    ModuleName = md.Module.Name,
                    ModuleDescription = md.Module.Description,
                    ModuleUrl = md.Module.Url,
                    ServiceId = md.Module.ServiceId,
                    ServiceName = md.ServiceName,
                    IsAlreadyIncluded = assignedModuleIds.Contains(md.Module.Id),
                    IsAvailable = !assignedModuleIds.Contains(md.Module.Id),
                    UnavailableReason = assignedModuleIds.Contains(md.Module.Id)
                        ? "Module is already assigned to this CustomPlan"
                        : null,
                })
                .ToList();

            return new ApiResponse<IEnumerable<ModuleAvailabilityDTO>>(
                true,
                "Available modules for CustomPlan retrieved successfully",
                availabilityDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting available modules for CustomPlan: {CustomPlanId}",
                request.CustomPlanId
            );
            return new ApiResponse<IEnumerable<ModuleAvailabilityDTO>>(
                false,
                "Error retrieving available modules",
                null!
            );
        }
    }
}
