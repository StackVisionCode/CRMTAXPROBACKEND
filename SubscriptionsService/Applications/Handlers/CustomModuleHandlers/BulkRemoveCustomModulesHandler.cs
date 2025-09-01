using Commands.CustomModuleCommands;
using Common;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CustomModuleHandlers;

/// Handler para remover múltiples módulos de un CustomPlan
public class BulkRemoveCustomModulesHandler
    : IRequestHandler<BulkRemoveCustomModulesCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BulkRemoveCustomModulesHandler> _logger;

    public BulkRemoveCustomModulesHandler(
        ApplicationDbContext dbContext,
        ILogger<BulkRemoveCustomModulesHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        BulkRemoveCustomModulesCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Verificar que el CustomPlan existe
            var customPlanExists = await _dbContext.CustomPlans.AnyAsync(
                cp => cp.Id == request.CustomPlanId,
                cancellationToken
            );

            if (!customPlanExists)
            {
                _logger.LogWarning("CustomPlan not found: {CustomPlanId}", request.CustomPlanId);
                return new ApiResponse<bool>(false, "CustomPlan not found", false);
            }

            // 2. Obtener CustomModules a eliminar
            var customModulesToRemove = await _dbContext
                .CustomModules.Where(cm =>
                    cm.CustomPlanId == request.CustomPlanId
                    && request.ModuleIds.Contains(cm.ModuleId)
                )
                .ToListAsync(cancellationToken);

            if (!customModulesToRemove.Any())
            {
                return new ApiResponse<bool>(false, "No CustomModules found to remove", false);
            }

            // 3. Eliminar CustomModules
            _dbContext.CustomModules.RemoveRange(customModulesToRemove);

            // 4. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<bool>(false, "Failed to remove CustomModules", false);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Bulk removed {Count} CustomModules from plan: {CustomPlanId}",
                customModulesToRemove.Count,
                request.CustomPlanId
            );

            return new ApiResponse<bool>(
                true,
                $"Removed {customModulesToRemove.Count} CustomModules successfully",
                true
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error bulk removing CustomModules");
            return new ApiResponse<bool>(false, "Error removing CustomModules", false);
        }
    }
}
