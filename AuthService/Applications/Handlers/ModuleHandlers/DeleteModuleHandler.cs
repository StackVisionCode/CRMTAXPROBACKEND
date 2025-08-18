using Commands.ModuleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.ModuleHandlers;

public class DeleteModuleHandler : IRequestHandler<DeleteModuleCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteModuleHandler> _logger;

    public DeleteModuleHandler(ApplicationDbContext dbContext, ILogger<DeleteModuleHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteModuleCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Verificar que el Module existe
            var module = await _dbContext.Modules.FirstOrDefaultAsync(
                m => m.Id == request.ModuleId,
                cancellationToken
            );

            if (module == null)
            {
                _logger.LogWarning("Module not found: {ModuleId}", request.ModuleId);
                return new ApiResponse<bool>(false, "Module not found", false);
            }

            // 2. Verificar si hay CustomModules usando este Module
            var customModulesUsingQuery =
                from cm in _dbContext.CustomModules
                where cm.ModuleId == module.Id && cm.IsIncluded
                select cm.Id;

            var customModulesCount = await customModulesUsingQuery.CountAsync(cancellationToken);
            if (customModulesCount > 0)
            {
                // Soft delete - solo desactivar
                module.IsActive = false;
                module.DeleteAt = DateTime.UtcNow;
                module.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Module soft deleted due to {Count} CustomModules using it: {ModuleId}",
                    customModulesCount,
                    request.ModuleId
                );
            }
            else
            {
                // Hard delete - eliminar completamente
                _dbContext.Modules.Remove(module);
                _logger.LogInformation("Module hard deleted: {ModuleId}", request.ModuleId);
            }

            // 3. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<bool>(false, "Failed to delete Module", false);
            }

            await transaction.CommitAsync(cancellationToken);

            var message =
                customModulesCount > 0
                    ? $"Module deactivated (used by {customModulesCount} custom plans)"
                    : "Module deleted successfully";

            return new ApiResponse<bool>(true, message, true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error deleting Module: {ModuleId}", request.ModuleId);
            return new ApiResponse<bool>(false, "Error deleting Module", false);
        }
    }
}
