using Commands.ServiceCommands;
using Common;
using Infrastructure.Context;
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
            var service = await _dbContext.Services.FirstOrDefaultAsync(
                s => s.Id == request.ServiceId,
                cancellationToken
            );

            if (service == null)
            {
                return new ApiResponse<bool>(false, "Service not found", false);
            }

            // Verificar si hay CustomPlans usando este Service (solo en SubscriptionsService)
            var customPlansUsingService = await _dbContext
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
                .Where(x => x.m.ServiceId == service.Id && x.cm.IsIncluded)
                .CountAsync(cancellationToken);

            if (customPlansUsingService > 0)
            {
                // Soft delete - solo desactivar
                service.IsActive = false;
                service.DeleteAt = DateTime.UtcNow;
                service.UpdatedAt = DateTime.UtcNow;

                _logger.LogInformation(
                    "Service soft deleted due to {Count} custom plans using it: {ServiceId}",
                    customPlansUsingService,
                    request.ServiceId
                );
            }
            else
            {
                // Hard delete
                _dbContext.Services.Remove(service);
                _logger.LogInformation("Service hard deleted: {ServiceId}", request.ServiceId);
            }

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<bool>(false, "Failed to delete Service", false);
            }

            await transaction.CommitAsync(cancellationToken);

            var message =
                customPlansUsingService > 0
                    ? $"Service deactivated (used by {customPlansUsingService} custom plans)"
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
