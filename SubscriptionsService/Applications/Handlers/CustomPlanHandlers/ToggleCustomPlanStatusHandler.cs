using Commands.CustomPlanCommands;
using Common;
using DTOs.CustomPlanDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CustomPlanHandlers;

/// Handler para activar/desactivar CustomPlan
public class ToggleCustomPlanStatusHandler
    : IRequestHandler<ToggleCustomPlanStatusCommand, ApiResponse<CustomPlanDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ToggleCustomPlanStatusHandler> _logger;

    public ToggleCustomPlanStatusHandler(
        ApplicationDbContext dbContext,
        ILogger<ToggleCustomPlanStatusHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CustomPlanDTO>> Handle(
        ToggleCustomPlanStatusCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var customPlan = await _dbContext.CustomPlans.FirstOrDefaultAsync(
                cp => cp.Id == request.CustomPlanId,
                cancellationToken
            );

            if (customPlan == null)
            {
                return new ApiResponse<CustomPlanDTO>(false, "CustomPlan not found", null!);
            }

            // Actualizar estado
            customPlan.IsActive = request.IsActive;
            customPlan.UpdatedAt = DateTime.UtcNow;

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                return new ApiResponse<CustomPlanDTO>(
                    false,
                    "Failed to update CustomPlan status",
                    null!
                );
            }

            // Construir respuesta
            var response = new CustomPlanDTO
            {
                Id = customPlan.Id,
                CompanyId = customPlan.CompanyId,
                Price = customPlan.Price,
                UserLimit = customPlan.UserLimit,
                IsActive = customPlan.IsActive,
                StartDate = customPlan.StartDate,
                RenewDate = customPlan.RenewDate,
                isRenewed = customPlan.IsRenewed,
                RenewedDate = customPlan.RenewedDate,
                AdditionalModuleNames = new List<string>(),
            };

            var action = request.IsActive ? "activated" : "deactivated";
            _logger.LogInformation("CustomPlan {Action}: {CustomPlanId}", action, customPlan.Id);

            return new ApiResponse<CustomPlanDTO>(
                true,
                $"CustomPlan {action} successfully",
                response
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error toggling CustomPlan status: {CustomPlanId}",
                request.CustomPlanId
            );
            return new ApiResponse<CustomPlanDTO>(false, "Error updating CustomPlan status", null!);
        }
    }
}
