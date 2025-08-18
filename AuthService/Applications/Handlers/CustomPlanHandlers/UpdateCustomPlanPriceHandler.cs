using AuthService.DTOs.CustomModuleDTOs;
using AuthService.DTOs.CustomPlanDTOs;
using Commands.CustomPlanCommands;
using Common;
using Infraestructure.Context;
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

            // 2. Validar precio
            if (request.NewPrice < 0)
            {
                return new ApiResponse<CustomPlanDTO>(false, "Price cannot be negative", null!);
            }

            // 3. Actualizar precio
            customPlan.Price = request.NewPrice; // 🔧 CORREGIDO: usar request.NewPrice directo
            customPlan.UpdatedAt = DateTime.UtcNow;

            // 4. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<CustomPlanDTO>(
                    false,
                    "Failed to update CustomPlan price",
                    null!
                );
            }

            await transaction.CommitAsync(cancellationToken);

            // 5. Obtener CustomPlan actualizado para respuesta
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
                "CustomPlan price updated successfully: {CustomPlanId}, NewPrice: {Price}",
                customPlan.Id,
                request.NewPrice
            );

            return new ApiResponse<CustomPlanDTO>(
                true,
                "CustomPlan price updated successfully",
                customPlanDto
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error updating CustomPlan price: {CustomPlanId}",
                request.CustomPlanId
            );
            return new ApiResponse<CustomPlanDTO>(false, "Error updating CustomPlan price", null!);
        }
    }
}
