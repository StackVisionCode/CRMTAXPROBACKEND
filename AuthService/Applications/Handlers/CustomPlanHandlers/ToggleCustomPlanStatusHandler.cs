using AuthService.DTOs.CustomModuleDTOs;
using AuthService.DTOs.CustomPlanDTOs;
using Commands.CustomPlanCommands;
using Common;
using Infraestructure.Context;
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
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Verificar que el CustomPlan existe
            var customPlan = await _dbContext.CustomPlans.FirstOrDefaultAsync(
                cp => cp.Id == request.CustomPlanId,
                cancellationToken
            );

            if (customPlan == null)
            {
                _logger.LogWarning("CustomPlan not found: {CustomPlanId}", request.CustomPlanId);
                return new ApiResponse<CustomPlanDTO>(false, "CustomPlan not found", null!);
            }

            // 2. Actualizar estado
            customPlan.IsActive = request.IsActive;
            customPlan.UpdatedAt = DateTime.UtcNow;

            // 3. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<CustomPlanDTO>(
                    false,
                    "Failed to update CustomPlan status",
                    null!
                );
            }

            await transaction.CommitAsync(cancellationToken);

            // 4. Obtener CustomPlan actualizado para respuesta
            var updatedPlanQuery =
                from cp in _dbContext.CustomPlans
                join c in _dbContext.Companies on cp.CompanyId equals c.Id
                where cp.Id == customPlan.Id
                select new
                {
                    CustomPlan = cp,
                    Company = c,
                    ModuleNames = (
                        from cm in _dbContext.CustomModules
                        join m in _dbContext.Modules on cm.ModuleId equals m.Id
                        where cm.CustomPlanId == cp.Id && cm.IsIncluded
                        select m.Name
                    ).ToList(),
                };

            var planData = await updatedPlanQuery.FirstOrDefaultAsync(cancellationToken);

            var customPlanDto = new CustomPlanDTO
            {
                Id = planData!.CustomPlan.Id,
                CompanyId = planData.CustomPlan.CompanyId,
                UserLimit = planData.CustomPlan.UserLimit,
                Price = planData.CustomPlan.Price,
                IsActive = planData.CustomPlan.IsActive,
                StartDate = planData.CustomPlan.StartDate,
                isRenewed = planData.CustomPlan.isRenewed,
                RenewedDate = planData.CustomPlan.RenewedDate,
                RenewDate = planData.CustomPlan.RenewDate,
                CompanyName = planData.Company.IsCompany
                    ? planData.Company.CompanyName
                    : planData.Company.FullName,
                CompanyDomain = planData.Company.Domain,
                AdditionalModuleNames = planData.ModuleNames,
                CustomModules = new List<CustomModuleDTO>(),
            };

            var action = request.IsActive ? "activated" : "deactivated";
            _logger.LogInformation("CustomPlan {Action}: {CustomPlanId}", action, customPlan.Id);

            return new ApiResponse<CustomPlanDTO>(
                true,
                $"CustomPlan {action} successfully",
                customPlanDto
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error toggling CustomPlan status: {CustomPlanId}",
                request.CustomPlanId
            );
            return new ApiResponse<CustomPlanDTO>(false, "Error updating CustomPlan status", null!);
        }
    }
}
