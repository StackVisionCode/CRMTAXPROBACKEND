using AuthService.Domains.CustomPlans;
using Commands.CustomPlanCommands;
using Common;
using Infraestructure.Context;
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
            // 1. CORREGIDO: Obtener CustomPlan con análisis completo
            var customPlanAnalysisQuery =
                from cp in _dbContext.CustomPlans
                join c in _dbContext.Companies on cp.CompanyId equals c.Id
                where cp.Id == request.CustomPlanId
                select new
                {
                    CustomPlan = cp,
                    Company = c,
                    // Solo TaxUsers ahora
                    TotalTaxUsers = _dbContext.TaxUsers.Count(u => u.CompanyId == cp.CompanyId),
                    ActiveTaxUsers = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == cp.CompanyId && u.IsActive
                    ),
                    OwnerCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == cp.CompanyId && u.IsOwner
                    ),
                    // Otros datos para validación
                    CustomModulesCount = _dbContext.CustomModules.Count(cm =>
                        cm.CustomPlanId == cp.Id
                    ),
                    ActiveSessionsCount = (
                        from s in _dbContext.Sessions
                        join u in _dbContext.TaxUsers on s.TaxUserId equals u.Id
                        where u.CompanyId == cp.CompanyId && !s.IsRevoke
                        select s.Id
                    ).Count(),
                };

            var analysis = await customPlanAnalysisQuery.FirstOrDefaultAsync(cancellationToken);
            if (analysis?.CustomPlan == null)
            {
                _logger.LogWarning("CustomPlan not found: {CustomPlanId}", request.CustomPlanId);
                return new ApiResponse<bool>(false, "CustomPlan not found", false);
            }

            var customPlan = analysis.CustomPlan;
            var company = analysis.Company;

            _logger.LogInformation(
                "Analyzing CustomPlan for deletion: {CustomPlanId}, Company: {CompanyId}, "
                    + "TaxUsers: {TotalUsers} ({ActiveUsers} active, {OwnerCount} owners), "
                    + "Modules: {ModulesCount}, Sessions: {SessionsCount}",
                request.CustomPlanId,
                company.Id,
                analysis.TotalTaxUsers,
                analysis.ActiveTaxUsers,
                analysis.OwnerCount,
                analysis.CustomModulesCount,
                analysis.ActiveSessionsCount
            );

            // 2. CORREGIDO: Validaciones actualizadas
            var validationResult = ValidateCustomPlanDeletion(analysis);
            if (!validationResult.CanDelete)
            {
                _logger.LogWarning(
                    "Cannot delete CustomPlan {CustomPlanId}: {Reason}",
                    request.CustomPlanId,
                    validationResult.Reason
                );
                return new ApiResponse<bool>(false, validationResult.Reason, false);
            }

            // 3. Eliminar en orden correcto

            // 3a. Eliminar CustomModules asociados
            var customModules = await _dbContext
                .CustomModules.Where(cm => cm.CustomPlanId == request.CustomPlanId)
                .ToListAsync(cancellationToken);

            if (customModules.Any())
            {
                _dbContext.CustomModules.RemoveRange(customModules);
                _logger.LogDebug("Marked {Count} CustomModules for deletion", customModules.Count);
            }

            // 3b. Company debe tener un CustomPlan válido
            // En lugar de poner Guid.Empty, necesitamos crear un plan básico o manejar esto
            // según tu lógica de negocio
            if (company != null)
            {
                // Opción 1: Crear un plan básico por defecto
                var basicPlan = await CreateBasicCustomPlanAsync(company.Id, cancellationToken);
                if (basicPlan != null)
                {
                    company.CustomPlanId = basicPlan.Id;
                    _logger.LogInformation(
                        "Created basic CustomPlan {NewPlanId} for Company {CompanyId}",
                        basicPlan.Id,
                        company.Id
                    );
                }
                else
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new ApiResponse<bool>(
                        false,
                        "Failed to create replacement CustomPlan for Company",
                        false
                    );
                }

                company.UpdatedAt = DateTime.UtcNow;
            }

            // 3c. Eliminar CustomPlan original
            _dbContext.CustomPlans.Remove(customPlan);

            // 4. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<bool>(false, "Failed to delete CustomPlan", false);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "CustomPlan deleted successfully: {CustomPlanId}, Company moved to basic plan",
                request.CustomPlanId
            );

            return new ApiResponse<bool>(true, "CustomPlan deleted successfully", true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error deleting CustomPlan: {CustomPlanId}", request.CustomPlanId);
            return new ApiResponse<bool>(false, "Error deleting CustomPlan", false);
        }
    }

    #region Helper Methods

    /// <summary>
    /// ACTUALIZADO: Valida si el CustomPlan puede ser eliminado
    /// </summary>
    private (bool CanDelete, string Reason) ValidateCustomPlanDeletion(dynamic analysis)
    {
        // Verificar usuarios activos (solo TaxUsers)
        if (analysis.ActiveTaxUsers > 1) // Más que solo el Owner
        {
            return (
                false,
                $"Cannot delete CustomPlan. Company has {analysis.ActiveTaxUsers} active users. "
                    + "Only companies with 1 or no active users can have their plan deleted."
            );
        }

        // Verificar que hay al menos un Owner
        if (analysis.OwnerCount == 0)
        {
            return (
                false,
                "Cannot delete CustomPlan. Company has no Owner. Please assign an Owner first."
            );
        }

        // Verificar sesiones activas
        if (analysis.ActiveSessionsCount > 0)
        {
            return (
                false,
                $"Cannot delete CustomPlan. Company has {analysis.ActiveSessionsCount} active sessions. "
                    + "Please wait for sessions to expire or revoke them first."
            );
        }

        // Verificar que no es un plan crítico del sistema
        if (analysis.CustomPlan.Price == 0m)
        {
            return (false, "Cannot delete system or free CustomPlan. These plans are protected.");
        }

        return (true, "CustomPlan can be safely deleted");
    }

    /// <summary>
    /// Crea un CustomPlan básico de reemplazo
    /// </summary>
    private async Task<CustomPlan?> CreateBasicCustomPlanAsync(Guid companyId, CancellationToken ct)
    {
        try
        {
            // Obtener el servicio Basic
            var basicServiceQuery =
                from s in _dbContext.Services
                where s.Name == "Basic" && s.IsActive
                select s;

            var basicService = await basicServiceQuery.FirstOrDefaultAsync(ct);
            if (basicService == null)
            {
                _logger.LogError("Basic service not found for creating replacement CustomPlan");
                return null;
            }

            // Crear nuevo CustomPlan básico con UserLimit
            var basicCustomPlan = new AuthService.Domains.CustomPlans.CustomPlan
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Price = basicService.Price,
                UserLimit = basicService.UserLimit,
                IsActive = true,
                StartDate = DateTime.UtcNow,
                RenewDate = DateTime.UtcNow.AddYears(1),
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.CustomPlans.AddAsync(basicCustomPlan, ct);

            // Resto del método sin cambios...
            var basicModulesQuery =
                from m in _dbContext.Modules
                where m.ServiceId == basicService.Id && m.IsActive
                select m;

            var basicModules = await basicModulesQuery.ToListAsync(ct);

            foreach (var module in basicModules)
            {
                var customModule = new AuthService.Domains.Modules.CustomModule
                {
                    Id = Guid.NewGuid(),
                    CustomPlanId = basicCustomPlan.Id,
                    ModuleId = module.Id,
                    IsIncluded = true,
                    CreatedAt = DateTime.UtcNow,
                };

                await _dbContext.CustomModules.AddAsync(customModule, ct);
            }

            _logger.LogDebug(
                "Created basic CustomPlan {PlanId} with {ModuleCount} modules, UserLimit: {UserLimit}",
                basicCustomPlan.Id,
                basicModules.Count,
                basicCustomPlan.UserLimit
            );

            return basicCustomPlan;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error creating basic CustomPlan for Company: {CompanyId}",
                companyId
            );
            return null;
        }
    }

    #endregion
}
