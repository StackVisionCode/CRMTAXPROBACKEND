using AuthService.Domains.Modules;
using AuthService.DTOs.ModuleDTOs;
using Commands.ModuleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.ModuleHandlers;

/// <summary>
/// Handler para crear un nuevo Module
/// </summary>
public class CreateModuleHandler : IRequestHandler<CreateModuleCommand, ApiResponse<ModuleDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CreateModuleHandler> _logger;

    public CreateModuleHandler(ApplicationDbContext dbContext, ILogger<CreateModuleHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<ModuleDTO>> Handle(
        CreateModuleCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dto = request.ModuleData;

            // 1. Verificar que el nombre no existe
            var nameExists = await _dbContext.Modules.AnyAsync(
                m => m.Name == dto.Name,
                cancellationToken
            );

            if (nameExists)
            {
                _logger.LogWarning("Module name already exists: {Name}", dto.Name);
                return new ApiResponse<ModuleDTO>(false, "Module name already exists", null!);
            }

            // 2. Verificar que el Service existe (si se proporciona)
            if (dto.ServiceId.HasValue)
            {
                var serviceExists = await _dbContext.Services.AnyAsync(
                    s => s.Id == dto.ServiceId.Value,
                    cancellationToken
                );

                if (!serviceExists)
                {
                    _logger.LogWarning("Service not found: {ServiceId}", dto.ServiceId);
                    return new ApiResponse<ModuleDTO>(false, "Service not found", null!);
                }
            }

            // 3. Crear Module
            var module = new Module
            {
                Id = Guid.NewGuid(),
                Name = dto.Name.Trim(),
                Description = dto.Description.Trim(),
                Url = dto.Url?.Trim(),
                IsActive = dto.IsActive,
                ServiceId = dto.ServiceId,
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.Modules.AddAsync(module, cancellationToken);

            // 4. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<ModuleDTO>(false, "Failed to create Module", null!);
            }

            await transaction.CommitAsync(cancellationToken);

            // 5. Obtener Module completo para respuesta
            var createdModuleQuery =
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

            var moduleData = await createdModuleQuery.FirstOrDefaultAsync(cancellationToken);

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

            _logger.LogInformation("Module created successfully: {ModuleId}", module.Id);

            return new ApiResponse<ModuleDTO>(true, "Module created successfully", moduleDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating Module");
            return new ApiResponse<ModuleDTO>(false, "Error creating Module", null!);
        }
    }
}
