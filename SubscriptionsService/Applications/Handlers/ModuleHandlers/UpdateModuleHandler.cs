using Commands.ModuleCommands;
using Common;
using DTOs.ModuleDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.ModuleHandlers;

public class UpdateModuleHandler : IRequestHandler<UpdateModuleCommand, ApiResponse<ModuleDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UpdateModuleHandler> _logger;

    public UpdateModuleHandler(ApplicationDbContext dbContext, ILogger<UpdateModuleHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<ModuleDTO>> Handle(
        UpdateModuleCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dto = request.ModuleData;

            // 1. Verificar que el Module existe
            var module = await _dbContext.Modules.FirstOrDefaultAsync(
                m => m.Id == dto.Id,
                cancellationToken
            );

            if (module == null)
            {
                _logger.LogWarning("Module not found: {ModuleId}", dto.Id);
                return new ApiResponse<ModuleDTO>(false, "Module not found", null!);
            }

            // 2. Verificar que el nombre no existe en otro Module
            var nameExists = await _dbContext.Modules.AnyAsync(
                m => m.Name == dto.Name && m.Id != dto.Id,
                cancellationToken
            );

            if (nameExists)
            {
                _logger.LogWarning("Module name already exists: {Name}", dto.Name);
                return new ApiResponse<ModuleDTO>(false, "Module name already exists", null!);
            }

            // 3. Actualizar Module
            module.Name = dto.Name.Trim();
            module.Description = dto.Description.Trim();
            module.Url = dto.Url?.Trim();
            module.IsActive = dto.IsActive;
            module.UpdatedAt = DateTime.UtcNow;

            // 4. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<ModuleDTO>(false, "Failed to update Module", null!);
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

            _logger.LogInformation("Module updated successfully: {ModuleId}", module.Id);

            return new ApiResponse<ModuleDTO>(true, "Module updated successfully", moduleDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error updating Module: {ModuleId}", request.ModuleData.Id);
            return new ApiResponse<ModuleDTO>(false, "Error updating Module", null!);
        }
    }
}
