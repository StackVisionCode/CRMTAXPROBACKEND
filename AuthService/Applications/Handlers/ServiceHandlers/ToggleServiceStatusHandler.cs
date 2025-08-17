using AuthService.DTOs.ServiceDTOs;
using Commands.ServiceCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.ServiceHandlers;

public class ToggleServiceStatusHandler
    : IRequestHandler<ToggleServiceStatusCommand, ApiResponse<ServiceDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ToggleServiceStatusHandler> _logger;

    public ToggleServiceStatusHandler(
        ApplicationDbContext dbContext,
        ILogger<ToggleServiceStatusHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<ServiceDTO>> Handle(
        ToggleServiceStatusCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Verificar que el Service existe
            var service = await _dbContext.Services.FirstOrDefaultAsync(
                s => s.Id == request.ServiceId,
                cancellationToken
            );

            if (service == null)
            {
                _logger.LogWarning("Service not found: {ServiceId}", request.ServiceId);
                return new ApiResponse<ServiceDTO>(false, "Service not found", null!);
            }

            // 2. Si se va a desactivar, verificar impacto en Companies
            if (!request.IsActive && service.IsActive)
            {
                var companiesAffectedQuery =
                    from c in _dbContext.Companies
                    join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                    join cm in _dbContext.CustomModules on cp.Id equals cm.CustomPlanId
                    join m in _dbContext.Modules on cm.ModuleId equals m.Id
                    where m.ServiceId == service.Id && cm.IsIncluded && cp.IsActive
                    select c.Id;

                var affectedCompanies = await companiesAffectedQuery.CountAsync(cancellationToken);
                if (affectedCompanies > 0)
                {
                    return new ApiResponse<ServiceDTO>(
                        false,
                        $"Cannot deactivate service. {affectedCompanies} companies are currently using it.",
                        null!
                    );
                }
            }

            // 3. Actualizar estado
            service.IsActive = request.IsActive;
            service.UpdatedAt = DateTime.UtcNow;

            // 4. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<ServiceDTO>(false, "Failed to update Service status", null!);
            }

            await transaction.CommitAsync(cancellationToken);

            // 5. Obtener Service actualizado para respuesta
            var updatedServiceQuery =
                from s in _dbContext.Services
                where s.Id == service.Id
                select new
                {
                    Service = s,
                    ModuleNames = (
                        from m in _dbContext.Modules
                        where m.ServiceId == s.Id
                        select m.Name
                    ).ToList(),
                    ModuleIds = (
                        from m in _dbContext.Modules
                        where m.ServiceId == s.Id
                        select m.Id
                    ).ToList(),
                };

            var serviceData = await updatedServiceQuery.FirstOrDefaultAsync(cancellationToken);

            // 6. Mapear con nuevos campos
            var serviceDto = new ServiceDTO
            {
                Id = serviceData!.Service.Id,
                Name = serviceData.Service.Name,
                Title = serviceData.Service.Title,
                Description = serviceData.Service.Description,
                Features = serviceData.Service.Features,
                Price = serviceData.Service.Price,
                UserLimit = serviceData.Service.UserLimit,
                IsActive = serviceData.Service.IsActive,
                ModuleNames = serviceData.ModuleNames,
                ModuleIds = serviceData.ModuleIds,
                CreatedAt = serviceData.Service.CreatedAt,
            };

            var action = request.IsActive ? "activated" : "deactivated";
            _logger.LogInformation("Service {Action}: {ServiceId}", action, service.Id);

            return new ApiResponse<ServiceDTO>(true, $"Service {action} successfully", serviceDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error toggling Service status: {ServiceId}", request.ServiceId);
            return new ApiResponse<ServiceDTO>(false, "Error updating Service status", null!);
        }
    }
}
