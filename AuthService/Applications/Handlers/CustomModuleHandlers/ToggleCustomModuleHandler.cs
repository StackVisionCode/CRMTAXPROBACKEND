using AuthService.DTOs.CustomModuleDTOs;
using Commands.CustomModuleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CustomModuleHandlers;

/// Handler para activar/desactivar CustomModule
public class ToggleCustomModuleHandler
    : IRequestHandler<ToggleCustomModuleCommand, ApiResponse<CustomModuleDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ToggleCustomModuleHandler> _logger;

    public ToggleCustomModuleHandler(
        ApplicationDbContext dbContext,
        ILogger<ToggleCustomModuleHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CustomModuleDTO>> Handle(
        ToggleCustomModuleCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Verificar que el CustomModule existe
            var customModule = await _dbContext.CustomModules.FirstOrDefaultAsync(
                cm => cm.Id == request.CustomModuleId,
                cancellationToken
            );

            if (customModule == null)
            {
                _logger.LogWarning(
                    "CustomModule not found: {CustomModuleId}",
                    request.CustomModuleId
                );
                return new ApiResponse<CustomModuleDTO>(false, "CustomModule not found", null!);
            }

            // 2. Actualizar estado
            customModule.IsIncluded = request.IsIncluded;
            customModule.UpdatedAt = DateTime.UtcNow;

            // 3. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<CustomModuleDTO>(
                    false,
                    "Failed to update CustomModule status",
                    null!
                );
            }

            await transaction.CommitAsync(cancellationToken);

            // 4. Obtener CustomModule actualizado para respuesta
            var updatedModuleQuery =
                from cm in _dbContext.CustomModules
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                where cm.Id == customModule.Id
                select new { CustomModule = cm, Module = m };

            var moduleData = await updatedModuleQuery.FirstOrDefaultAsync(cancellationToken);

            var customModuleDto = new CustomModuleDTO
            {
                Id = moduleData!.CustomModule.Id,
                CustomPlanId = moduleData.CustomModule.CustomPlanId,
                ModuleId = moduleData.CustomModule.ModuleId,
                IsIncluded = moduleData.CustomModule.IsIncluded,
                ModuleName = moduleData.Module.Name,
                ModuleDescription = moduleData.Module.Description,
                ModuleUrl = moduleData.Module.Url,
            };

            var action = request.IsIncluded ? "included" : "excluded";
            _logger.LogInformation(
                "CustomModule {Action}: {CustomModuleId}",
                action,
                customModule.Id
            );

            return new ApiResponse<CustomModuleDTO>(
                true,
                $"CustomModule {action} successfully",
                customModuleDto
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error toggling CustomModule: {CustomModuleId}",
                request.CustomModuleId
            );
            return new ApiResponse<CustomModuleDTO>(
                false,
                "Error updating CustomModule status",
                null!
            );
        }
    }
}
