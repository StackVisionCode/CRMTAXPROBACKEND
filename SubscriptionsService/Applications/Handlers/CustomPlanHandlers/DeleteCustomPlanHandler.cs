using Commands.CustomPlanCommands;
using Common;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CustomPlanHandlers;

public class DeleteCustomPlanHandler : IRequestHandler<DeleteCustomPlanCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteCustomPlanHandler> _logger;

    public DeleteCustomPlanHandler(
        ApplicationDbContext dbContext,
        ILogger<DeleteCustomPlanHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteCustomPlanCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var customPlan = await _dbContext.CustomPlans.FirstOrDefaultAsync(
                cp => cp.Id == request.CustomPlanId,
                cancellationToken
            );

            if (customPlan == null)
            {
                return new ApiResponse<bool>(false, "CustomPlan not found", false);
            }

            // 1. Eliminar CustomModules
            var customModules = await _dbContext
                .CustomModules.Where(cm => cm.CustomPlanId == request.CustomPlanId)
                .ToListAsync(cancellationToken);

            if (customModules.Any())
            {
                _dbContext.CustomModules.RemoveRange(customModules);
            }

            // 2. Eliminar CustomPlan
            _dbContext.CustomPlans.Remove(customPlan);

            // 3. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<bool>(false, "Failed to delete CustomPlan", false);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("CustomPlan deleted: {CustomPlanId}", request.CustomPlanId);
            return new ApiResponse<bool>(true, "CustomPlan deleted successfully", true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error deleting CustomPlan: {CustomPlanId}", request.CustomPlanId);
            return new ApiResponse<bool>(false, "Error deleting CustomPlan", false);
        }
    }
}
