using Commands.CustomPlanCommands;
using Common;
using DTOs.CustomPlanDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CustomPlanHandlers;

/// Handler para actualizar precio de CustomPlan
public class UpdateCustomPlanPriceHandler
    : IRequestHandler<UpdateCustomPlanPriceCommand, ApiResponse<CustomPlanDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UpdateCustomPlanPriceHandler> _logger;

    public UpdateCustomPlanPriceHandler(
        ApplicationDbContext dbContext,
        ILogger<UpdateCustomPlanPriceHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CustomPlanDTO>> Handle(
        UpdateCustomPlanPriceCommand request,
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

            if (request.NewPrice < 0)
            {
                return new ApiResponse<CustomPlanDTO>(false, "Price cannot be negative", null!);
            }

            // Actualizar precio
            var oldPrice = customPlan.Price;
            customPlan.Price = request.NewPrice;
            customPlan.UpdatedAt = DateTime.UtcNow;

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                return new ApiResponse<CustomPlanDTO>(
                    false,
                    "Failed to update CustomPlan price",
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

            _logger.LogInformation(
                "CustomPlan price updated: {CustomPlanId}, Old: {OldPrice}, New: {NewPrice}",
                customPlan.Id,
                oldPrice,
                request.NewPrice
            );

            return new ApiResponse<CustomPlanDTO>(
                true,
                "CustomPlan price updated successfully",
                response
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error updating CustomPlan price: {CustomPlanId}",
                request.CustomPlanId
            );
            return new ApiResponse<CustomPlanDTO>(false, "Error updating CustomPlan price", null!);
        }
    }
}
