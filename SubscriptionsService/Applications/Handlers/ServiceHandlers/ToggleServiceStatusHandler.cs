using Commands.ServiceCommands;
using Common;
using DTOs.ServiceDTOs;
using Infrastructure.Context;
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
        try
        {
            var service = await _dbContext.Services.FirstOrDefaultAsync(
                s => s.Id == request.ServiceId,
                cancellationToken
            );

            if (service == null)
            {
                return new ApiResponse<ServiceDTO>(false, "Service not found", null!);
            }

            // Si se va a desactivar, verificar solo CustomPlans activos
            if (!request.IsActive && service.IsActive)
            {
                var activePlansUsingService = await _dbContext
                    .CustomPlans.Join(
                        _dbContext.CustomModules,
                        cp => cp.Id,
                        cm => cm.CustomPlanId,
                        (cp, cm) => new { cp, cm }
                    )
                    .Join(
                        _dbContext.Modules,
                        x => x.cm.ModuleId,
                        m => m.Id,
                        (x, m) =>
                            new
                            {
                                x.cp,
                                x.cm,
                                m,
                            }
                    )
                    .Where(x => x.m.ServiceId == service.Id && x.cm.IsIncluded && x.cp.IsActive)
                    .CountAsync(cancellationToken);

                if (activePlansUsingService > 0)
                {
                    return new ApiResponse<ServiceDTO>(
                        false,
                        $"Cannot deactivate service. {activePlansUsingService} active custom plans are currently using it.",
                        null!
                    );
                }
            }

            // Actualizar estado
            service.IsActive = request.IsActive;
            service.UpdatedAt = DateTime.UtcNow;

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                return new ApiResponse<ServiceDTO>(false, "Failed to update Service status", null!);
            }

            // Construir respuesta simple
            var serviceDto = new ServiceDTO
            {
                Id = service.Id,
                Name = service.Name,
                Title = service.Title,
                Description = service.Description,
                Features = service.Features,
                Price = service.Price,
                UserLimit = service.UserLimit,
                IsActive = service.IsActive,
                CreatedAt = service.CreatedAt,
                ModuleNames = new List<string>(),
                ModuleIds = new List<Guid>(),
            };

            var action = request.IsActive ? "activated" : "deactivated";
            _logger.LogInformation("Service {Action}: {ServiceId}", action, service.Id);

            return new ApiResponse<ServiceDTO>(true, $"Service {action} successfully", serviceDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling Service status: {ServiceId}", request.ServiceId);
            return new ApiResponse<ServiceDTO>(false, "Error updating Service status", null!);
        }
    }
}
