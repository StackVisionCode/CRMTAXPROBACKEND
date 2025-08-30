using Commands.CustomModuleCommands;
using Common;
using DTOs.CustomModuleDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CustomModuleHandlers;

public class UpdateCustomModuleHandler
    : IRequestHandler<UpdateCustomModuleCommand, ApiResponse<CustomModuleDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UpdateCustomModuleHandler> _logger;

    public UpdateCustomModuleHandler(
        ApplicationDbContext dbContext,
        ILogger<UpdateCustomModuleHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CustomModuleDTO>> Handle(
        UpdateCustomModuleCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dto = request.CustomModuleData;

            // 1. Verificar que el CustomModule existe
            var customModule = await _dbContext.CustomModules.FirstOrDefaultAsync(
                cm => cm.Id == dto.Id,
                cancellationToken
            );

            if (customModule == null)
            {
                _logger.LogWarning("CustomModule not found: {CustomModuleId}", dto.Id);
                return new ApiResponse<CustomModuleDTO>(false, "CustomModule not found", null!);
            }

            // 2. Actualizar CustomModule
            customModule.IsIncluded = dto.IsIncluded;
            customModule.UpdatedAt = DateTime.UtcNow;

            // 3. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<CustomModuleDTO>(
                    false,
                    "Failed to update CustomModule",
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

            _logger.LogInformation(
                "CustomModule updated successfully: {CustomModuleId}",
                customModule.Id
            );

            return new ApiResponse<CustomModuleDTO>(
                true,
                "CustomModule updated successfully",
                customModuleDto
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error updating CustomModule: {CustomModuleId}",
                request.CustomModuleData.Id
            );
            return new ApiResponse<CustomModuleDTO>(false, "Error updating CustomModule", null!);
        }
    }
}
