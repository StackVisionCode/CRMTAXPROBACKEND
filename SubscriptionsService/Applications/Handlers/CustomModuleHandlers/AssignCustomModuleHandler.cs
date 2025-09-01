using Commands.CustomModuleCommands;
using Common;
using Domains;
using DTOs.CustomModuleDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CustomModuleHandlers;

/// Handler para asignar un Module a un CustomPlan
public class AssignCustomModuleHandler
    : IRequestHandler<AssignCustomModuleCommand, ApiResponse<CustomModuleDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AssignCustomModuleHandler> _logger;

    public AssignCustomModuleHandler(
        ApplicationDbContext dbContext,
        ILogger<AssignCustomModuleHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CustomModuleDTO>> Handle(
        AssignCustomModuleCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dto = request.CustomModuleData;

            // 1. Verificar que el CustomPlan existe y está activo
            var customPlan = await _dbContext.CustomPlans.FirstOrDefaultAsync(
                cp => cp.Id == dto.CustomPlanId,
                cancellationToken
            );

            if (customPlan == null)
            {
                _logger.LogWarning("CustomPlan not found: {CustomPlanId}", dto.CustomPlanId);
                return new ApiResponse<CustomModuleDTO>(false, "CustomPlan not found", null!);
            }

            if (!customPlan.IsActive)
            {
                _logger.LogWarning("CustomPlan is not active: {CustomPlanId}", dto.CustomPlanId);
                return new ApiResponse<CustomModuleDTO>(false, "CustomPlan is not active", null!);
            }

            // 2. Verificar que el Module existe y está activo
            var module = await _dbContext.Modules.FirstOrDefaultAsync(
                m => m.Id == dto.ModuleId,
                cancellationToken
            );

            if (module == null)
            {
                _logger.LogWarning("Module not found: {ModuleId}", dto.ModuleId);
                return new ApiResponse<CustomModuleDTO>(false, "Module not found", null!);
            }

            if (!module.IsActive)
            {
                _logger.LogWarning("Module is not active: {ModuleId}", dto.ModuleId);
                return new ApiResponse<CustomModuleDTO>(false, "Module is not active", null!);
            }

            // 3. Verificar que no existe ya esta combinación
            var existingCustomModule = await _dbContext.CustomModules.FirstOrDefaultAsync(
                cm => cm.CustomPlanId == dto.CustomPlanId && cm.ModuleId == dto.ModuleId,
                cancellationToken
            );

            if (existingCustomModule != null)
            {
                _logger.LogWarning(
                    "CustomModule already exists: CustomPlan {CustomPlanId}, Module {ModuleId}",
                    dto.CustomPlanId,
                    dto.ModuleId
                );
                return new ApiResponse<CustomModuleDTO>(
                    false,
                    "Module is already assigned to this CustomPlan",
                    null!
                );
            }

            // 4. Crear CustomModule
            var customModule = new CustomModule
            {
                Id = Guid.NewGuid(),
                CustomPlanId = dto.CustomPlanId,
                ModuleId = dto.ModuleId,
                IsIncluded = dto.IsIncluded,
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.CustomModules.AddAsync(customModule, cancellationToken);

            // 5. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<CustomModuleDTO>(
                    false,
                    "Failed to assign CustomModule",
                    null!
                );
            }

            await transaction.CommitAsync(cancellationToken);

            // 6. Obtener CustomModule completo para respuesta
            var createdModuleQuery =
                from cm in _dbContext.CustomModules
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                where cm.Id == customModule.Id
                select new { CustomModule = cm, Module = m };

            var moduleData = await createdModuleQuery.FirstOrDefaultAsync(cancellationToken);
            if (moduleData?.CustomModule == null)
            {
                _logger.LogError(
                    "Failed to retrieve created CustomModule: {CustomModuleId}",
                    customModule.Id
                );
                return new ApiResponse<CustomModuleDTO>(
                    false,
                    "Failed to retrieve created CustomModule",
                    null!
                );
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

            _logger.LogInformation(
                "CustomModule assigned successfully: {CustomModuleId}",
                customModule.Id
            );

            return new ApiResponse<CustomModuleDTO>(
                true,
                "CustomModule assigned successfully",
                customModuleDto
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error assigning CustomModule");
            return new ApiResponse<CustomModuleDTO>(false, "Error assigning CustomModule", null!);
        }
    }
}
