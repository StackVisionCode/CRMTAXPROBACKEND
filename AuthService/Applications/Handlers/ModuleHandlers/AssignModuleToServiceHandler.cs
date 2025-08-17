using AuthService.DTOs.ModuleDTOs;
using Commands.ModuleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.ModuleHandlers;

public class AssignModuleToServiceHandler
    : IRequestHandler<AssignModuleToServiceCommand, ApiResponse<ModuleDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AssignModuleToServiceHandler> _logger;

    public AssignModuleToServiceHandler(
        ApplicationDbContext dbContext,
        ILogger<AssignModuleToServiceHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<ModuleDTO>> Handle(
        AssignModuleToServiceCommand request,
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

            // 2. Verificar que el Service existe (si se proporciona)
            if (request.ServiceId.HasValue)
            {
                var serviceExists = await _dbContext.Services.AnyAsync(
                    s => s.Id == request.ServiceId.Value,
                    cancellationToken
                );

                if (!serviceExists)
                {
                    _logger.LogWarning("Service not found: {ServiceId}", request.ServiceId);
                    return new ApiResponse<ModuleDTO>(false, "Service not found", null!);
                }
            }

            // 3. Actualizar asignación
            module.ServiceId = request.ServiceId;
            module.UpdatedAt = DateTime.UtcNow;

            // 4. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<ModuleDTO>(
                    false,
                    "Failed to assign Module to Service",
                    null!
                );
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

            var action = request.ServiceId.HasValue
                ? "assigned to service"
                : "unassigned from service";
            _logger.LogInformation("Module {Action}: {ModuleId}", action, module.Id);

            return new ApiResponse<ModuleDTO>(true, $"Module {action} successfully", moduleDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error assigning Module to Service: {ModuleId}", request.ModuleId);
            return new ApiResponse<ModuleDTO>(false, "Error assigning Module to Service", null!);
        }
    }
}
