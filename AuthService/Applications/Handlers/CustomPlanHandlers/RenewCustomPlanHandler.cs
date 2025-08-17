using AuthService.DTOs.CustomModuleDTOs;
using AuthService.DTOs.CustomPlanDTOs;
using Commands.CustomPlanCommands;
using Common;
using Infraestructure.Context;
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

            // 2. Actualizar datos de renovación
            customPlan.isRenewed = true;
            customPlan.RenewedDate = DateTime.UtcNow;

            // Si se proporciona nueva fecha de renovación, usarla; sino, extender por un año
            if (request.NewEndDate.HasValue)
            {
                customPlan.RenewDate = request.NewEndDate.Value;
            }
            else
            {
                // Extender RenewDate por un año desde la fecha actual de renovación
                customPlan.RenewDate = customPlan.RenewDate.AddYears(1);
            }

            customPlan.UpdatedAt = DateTime.UtcNow;

            // 3. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<CustomPlanDTO>(false, "Failed to renew CustomPlan", null!);
            }

            await transaction.CommitAsync(cancellationToken);

            // 4. Obtener CustomPlan renovado para respuesta
            var renewedPlanQuery =
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

            var planData = await renewedPlanQuery.FirstOrDefaultAsync(cancellationToken);

            var customPlanDto = new CustomPlanDTO
            {
                Id = planData!.CustomPlan.Id,
                CompanyId = planData.CustomPlan.CompanyId,
                Price = planData.CustomPlan.Price,
                UserLimit = planData.CustomPlan.UserLimit,
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

            _logger.LogInformation(
                "CustomPlan renewed successfully: {CustomPlanId}",
                customPlan.Id
            );

            return new ApiResponse<CustomPlanDTO>(
                true,
                "CustomPlan renewed successfully",
                customPlanDto
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error renewing CustomPlan: {CustomPlanId}", request.CustomPlanId);
            return new ApiResponse<CustomPlanDTO>(false, "Error renewing CustomPlan", null!);
        }
    }
}
