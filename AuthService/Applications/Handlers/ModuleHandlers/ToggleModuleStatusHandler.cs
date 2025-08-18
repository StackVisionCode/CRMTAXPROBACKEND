using AuthService.DTOs.ModuleDTOs;
using Commands.ModuleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.ModuleHandlers;

public class ToggleModuleStatusHandler
    : IRequestHandler<ToggleModuleStatusCommand, ApiResponse<ModuleDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ToggleModuleStatusHandler> _logger;

    public ToggleModuleStatusHandler(
        ApplicationDbContext dbContext,
        ILogger<ToggleModuleStatusHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<ModuleDTO>> Handle(
        ToggleModuleStatusCommand request,
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
                return new ApiResponse<ModuleDTO>(false, "Module not found", null!);
            }

            // 2. Si se va a desactivar, verificar impacto en CustomPlans activos
            if (!request.IsActive && module.IsActive)
            {
                var activePlansAffectedQuery =
                    from cm in _dbContext.CustomModules
                    join cp in _dbContext.CustomPlans on cm.CustomPlanId equals cp.Id
                    where cm.ModuleId == module.Id && cm.IsIncluded && cp.IsActive
                    select cp.Id;

                var affectedPlans = await activePlansAffectedQuery.CountAsync(cancellationToken);
                if (affectedPlans > 0)
                {
                    return new ApiResponse<ModuleDTO>(
                        false,
                        $"Cannot deactivate module. {affectedPlans} active custom plans are using it.",
                        null!
                    );
                }
            }

            // 3. Actualizar estado
            module.IsActive = request.IsActive;
            module.UpdatedAt = DateTime.UtcNow;

            // 4. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<ModuleDTO>(false, "Failed to update Module status", null!);
            }

            await transaction.CommitAsync(cancellationToken);

            // 5. Obtener Module actualizado para respuesta
            var updatedModuleQuery =
                from m in _dbContext.Modules
                where m.Id == module.Id
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

            var moduleData = await updatedModuleQuery.FirstOrDefaultAsync(cancellationToken);

            var moduleDto = new ModuleDTO
            {
                Id = moduleData!.Module.Id,
                Name = moduleData.Module.Name,
                Description = moduleData.Module.Description,
                Url = moduleData.Module.Url,
                IsActive = moduleData.Module.IsActive,
                ServiceId = moduleData.Module.ServiceId,
                ServiceName = moduleData.ServiceName,
            };

            var action = request.IsActive ? "activated" : "deactivated";
            _logger.LogInformation("Module {Action}: {ModuleId}", action, module.Id);

            return new ApiResponse<ModuleDTO>(true, $"Module {action} successfully", moduleDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error toggling Module status: {ModuleId}", request.ModuleId);
            return new ApiResponse<ModuleDTO>(false, "Error updating Module status", null!);
        }
    }
}
