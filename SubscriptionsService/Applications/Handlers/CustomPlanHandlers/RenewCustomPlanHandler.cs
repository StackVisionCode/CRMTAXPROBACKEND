using Commands.CustomPlanCommands;
using Common;
using DTOs.CustomPlanDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CustomPlanHandlers;

public class RenewCustomPlanHandler
    : IRequestHandler<RenewCustomPlanCommand, ApiResponse<CustomPlanDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RenewCustomPlanHandler> _logger;

    public RenewCustomPlanHandler(
        ApplicationDbContext dbContext,
        ILogger<RenewCustomPlanHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CustomPlanDTO>> Handle(
        RenewCustomPlanCommand request,
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

            // Actualizar datos de renovación
            customPlan.IsRenewed = true;
            customPlan.RenewedDate = DateTime.UtcNow;

            // Actualizar fecha de renovación
            if (request.NewEndDate.HasValue)
            {
                customPlan.RenewDate = request.NewEndDate.Value;
            }
            else
            {
                customPlan.RenewDate = customPlan.RenewDate.AddYears(1);
            }

            customPlan.UpdatedAt = DateTime.UtcNow;

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                return new ApiResponse<CustomPlanDTO>(false, "Failed to renew CustomPlan", null!);
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
                "CustomPlan renewed: {CustomPlanId}, New end date: {RenewDate}",
                customPlan.Id,
                customPlan.RenewDate
            );

            return new ApiResponse<CustomPlanDTO>(
                true,
                "CustomPlan renewed successfully",
                response
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing CustomPlan: {CustomPlanId}", request.CustomPlanId);
            return new ApiResponse<CustomPlanDTO>(false, "Error renewing CustomPlan", null!);
        }
    }
}
