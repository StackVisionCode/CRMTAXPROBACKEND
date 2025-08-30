using Commands.CustomModuleCommands;
using Common;
using Domains;
using DTOs.CustomModuleDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CustomModuleHandlers;

/// Handler para asignar múltiples módulos a un CustomPlan
public class BulkAssignCustomModulesHandler
    : IRequestHandler<BulkAssignCustomModulesCommand, ApiResponse<IEnumerable<CustomModuleDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BulkAssignCustomModulesHandler> _logger;

    public BulkAssignCustomModulesHandler(
        ApplicationDbContext dbContext,
        ILogger<BulkAssignCustomModulesHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<CustomModuleDTO>>> Handle(
        BulkAssignCustomModulesCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Verificar que el CustomPlan existe y está activo
            var customPlan = await _dbContext.CustomPlans.FirstOrDefaultAsync(
                cp => cp.Id == request.CustomPlanId,
                cancellationToken
            );

            if (customPlan == null)
            {
                _logger.LogWarning("CustomPlan not found: {CustomPlanId}", request.CustomPlanId);
                return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                    false,
                    "CustomPlan not found",
                    null!
                );
            }

            if (!customPlan.IsActive)
            {
                _logger.LogWarning(
                    "CustomPlan is not active: {CustomPlanId}",
                    request.CustomPlanId
                );
                return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                    false,
                    "CustomPlan is not active",
                    null!
                );
            }

            // 2. Verificar que todos los módulos existen y están activos
            var modules = await _dbContext
                .Modules.Where(m => request.ModuleIds.Contains(m.Id))
                .ToListAsync(cancellationToken);

            if (modules.Count != request.ModuleIds.Count)
            {
                var foundIds = modules.Select(m => m.Id).ToList();
                var missingIds = request.ModuleIds.Except(foundIds).ToList();
                return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                    false,
                    $"Modules not found: {string.Join(", ", missingIds)}",
                    null!
                );
            }

            var inactiveModules = modules.Where(m => !m.IsActive).Select(m => m.Id).ToList();
            if (inactiveModules.Any())
            {
                return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                    false,
                    $"Inactive modules: {string.Join(", ", inactiveModules)}",
                    null!
                );
            }

            // 3. Verificar módulos ya asignados
            var existingCustomModules = await _dbContext
                .CustomModules.Where(cm =>
                    cm.CustomPlanId == request.CustomPlanId
                    && request.ModuleIds.Contains(cm.ModuleId)
                )
                .Select(cm => cm.ModuleId)
                .ToListAsync(cancellationToken);

            var newModuleIds = request.ModuleIds.Except(existingCustomModules).ToList();

            if (!newModuleIds.Any())
            {
                return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                    false,
                    "All modules are already assigned to this CustomPlan",
                    null!
                );
            }

            // 4. Crear CustomModules
            var customModules = newModuleIds
                .Select(moduleId => new CustomModule
                {
                    Id = Guid.NewGuid(),
                    CustomPlanId = request.CustomPlanId,
                    ModuleId = moduleId,
                    IsIncluded = true,
                    CreatedAt = DateTime.UtcNow,
                })
                .ToList();

            await _dbContext.CustomModules.AddRangeAsync(customModules, cancellationToken);

            // 5. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                    false,
                    "Failed to assign CustomModules",
                    null!
                );
            }

            await transaction.CommitAsync(cancellationToken);

            // 6. Obtener CustomModules creados para respuesta
            var createdModuleIds = customModules.Select(cm => cm.Id).ToList();
            var createdModulesQuery =
                from cm in _dbContext.CustomModules
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                where createdModuleIds.Contains(cm.Id)
                select new { CustomModule = cm, Module = m };

            var modulesData = await createdModulesQuery.ToListAsync(cancellationToken);

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

            _logger.LogInformation(
                "Bulk assigned {Count} CustomModules to plan: {CustomPlanId}",
                customModules.Count,
                request.CustomPlanId
            );

            return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                true,
                "CustomModules assigned successfully",
                customModulesDtos
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error bulk assigning CustomModules");
            return new ApiResponse<IEnumerable<CustomModuleDTO>>(
                false,
                "Error assigning CustomModules",
                null!
            );
        }
    }
}
