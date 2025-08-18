using Commands.ServiceCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.ServiceHandlers;

public class DeleteServiceHandler : IRequestHandler<DeleteServiceCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteServiceHandler> _logger;

    public DeleteServiceHandler(
        ApplicationDbContext dbContext,
        ILogger<DeleteServiceHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteServiceCommand request,
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
                return new ApiResponse<bool>(false, "Service not found", false);
            }

            // 2. Verificar si hay Companies usando este Service
            var companiesUsingServiceQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                join cm in _dbContext.CustomModules on cp.Id equals cm.CustomPlanId
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                where m.ServiceId == service.Id && cm.IsIncluded
                select c.Id;

            var companiesCount = await companiesUsingServiceQuery.CountAsync(cancellationToken);
            if (companiesCount > 0)
            {
                // Soft delete - solo desactivar
                service.IsActive = false;
                service.DeleteAt = DateTime.UtcNow;
                service.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Service soft deleted due to {Count} companies using it: {ServiceId}",
                    companiesCount,
                    request.ServiceId
                );
            }
            else
            {
                // Hard delete - eliminar completamente
                _dbContext.Services.Remove(service);
                _logger.LogInformation("Service hard deleted: {ServiceId}", request.ServiceId);
            }

            // 3. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<bool>(false, "Failed to delete Service", false);
            }

            await transaction.CommitAsync(cancellationToken);

            var message =
                companiesCount > 0
                    ? $"Service deactivated (used by {companiesCount} companies)"
                    : "Service deleted successfully";

            return new ApiResponse<bool>(true, message, true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error deleting Service: {ServiceId}", request.ServiceId);
            return new ApiResponse<bool>(false, "Error deleting Service", false);
        }
    }
}
