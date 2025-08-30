using Commands.CustomPlanCommands;
using Common;
using DTOs.CustomPlanDTOs;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CustomPlanHandlers;

public class UpdateCustomPlanHandler
    : IRequestHandler<UpdateCustomPlanCommand, ApiResponse<CustomPlanDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UpdateCustomPlanHandler> _logger;

    public UpdateCustomPlanHandler(
        ApplicationDbContext dbContext,
        ILogger<UpdateCustomPlanHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CustomPlanDTO>> Handle(
        UpdateCustomPlanCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dto = request.CustomPlanData;

            // 1. Verificar que existe el CustomPlan
            var customPlan = await _dbContext.CustomPlans.FirstOrDefaultAsync(
                cp => cp.Id == dto.Id,
                cancellationToken
            );

            if (customPlan == null)
            {
                return new ApiResponse<CustomPlanDTO>(false, "CustomPlan not found", null!);
            }

            // 2. Validar datos
            if (dto.Price < 0)
                return new ApiResponse<CustomPlanDTO>(false, "Price cannot be negative", null!);

            if (dto.UserLimit < 1)
                return new ApiResponse<CustomPlanDTO>(
                    false,
                    "User limit must be at least 1",
                    null!
                );

            // 3. Actualizar campos
            customPlan.Price = dto.Price;
            customPlan.UserLimit = dto.UserLimit;
            customPlan.IsActive = dto.IsActive;
            customPlan.StartDate = dto.StartDate;
            customPlan.RenewDate = dto.RenewDate;
            customPlan.IsRenewed = dto.isRenewed;
            customPlan.RenewedDate = dto.RenewedDate;
            customPlan.UpdatedAt = DateTime.UtcNow;

            // 4. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<CustomPlanDTO>(false, "Failed to update CustomPlan", null!);
            }

            await transaction.CommitAsync(cancellationToken);

            // 5. Construir respuesta
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

            _logger.LogInformation("CustomPlan updated: {CustomPlanId}", customPlan.Id);
            return new ApiResponse<CustomPlanDTO>(
                true,
                "CustomPlan updated successfully",
                response
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error updating CustomPlan: {CustomPlanId}",
                request.CustomPlanData.Id
            );
            return new ApiResponse<CustomPlanDTO>(false, "Error updating CustomPlan", null!);
        }
    }
}
