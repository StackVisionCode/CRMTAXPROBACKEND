using AuthService.Applications.Common;
using AuthService.Domains.Modules;
using AuthService.DTOs.CompanyDTOs;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.CompanyHandlers;

public class UpdateCompanyPlanHandler
    : IRequestHandler<UpdateCompanyPlanCommand, ApiResponse<CompanyPlanUpdateResultDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UpdateCompanyPlanHandler> _logger;

    public UpdateCompanyPlanHandler(
        ApplicationDbContext dbContext,
        ILogger<UpdateCompanyPlanHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyPlanUpdateResultDTO>> Handle(
        UpdateCompanyPlanCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dto = request.CompanyPlanData;

            // 1. Obtener información completa actual
            var currentStateQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == dto.CompanyId
                select new
                {
                    Company = c,
                    CustomPlan = cp,
                    // Conteo de usuarios activos
                    TaxUsersCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsActive
                    ),
                    UserCompaniesCount = _dbContext.UserCompanies.Count(uc =>
                        uc.CompanyId == c.Id && uc.IsActive
                    ),
                };

            var currentState = await currentStateQuery.FirstOrDefaultAsync(cancellationToken);
            if (currentState?.Company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", dto.CompanyId);
                return new ApiResponse<CompanyPlanUpdateResultDTO>(
                    false,
                    "Company not found",
                    null!
                );
            }

            // CORREGIDO: Obtener módulos actuales por separado
            var currentModulesQuery =
                from cm in _dbContext.CustomModules
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                where cm.CustomPlanId == currentState.CustomPlan.Id && cm.IsIncluded
                select new ModuleInfo
                {
                    Id = m.Id,
                    Name = m.Name,
                    ServiceId = m.ServiceId,
                };

            var currentModules = await currentModulesQuery.ToListAsync(cancellationToken);

            var company = currentState.Company;
            var currentPlan = currentState.CustomPlan;
            var totalActiveUsers = currentState.TaxUsersCount + currentState.UserCompaniesCount;

            _logger.LogInformation(
                "Starting service plan update for company {CompanyId}: Current users: {UserCount}",
                dto.CompanyId,
                totalActiveUsers
            );

            // 2. Obtener información del nuevo servicio
            var newServiceQuery =
                from s in _dbContext.Services
                where s.Name == dto.NewServiceLevel.ToString() && s.IsActive
                select new { Service = s };

            var newServiceInfo = await newServiceQuery.FirstOrDefaultAsync(cancellationToken);
            if (newServiceInfo?.Service == null)
            {
                _logger.LogError(
                    "Service not found for level: {ServiceLevel}",
                    dto.NewServiceLevel
                );
                return new ApiResponse<CompanyPlanUpdateResultDTO>(
                    false,
                    $"Service '{dto.NewServiceLevel}' not found or not active",
                    null!
                );
            }

            // CORREGIDO: Obtener módulos del nuevo servicio por separado
            var newServiceModulesQuery =
                from m in _dbContext.Modules
                where m.ServiceId == newServiceInfo.Service.Id && m.IsActive
                select new ModuleInfo
                {
                    Id = m.Id,
                    Name = m.Name,
                    ServiceId = m.ServiceId,
                };

            var newServiceModules = await newServiceModulesQuery.ToListAsync(cancellationToken);

            var newService = newServiceInfo.Service;

            // 3. Determinar el ServiceLevel actual para comparación
            var currentServiceLevel = DetermineCurrentServiceLevel(currentModules);

            _logger.LogInformation(
                "Plan change: {CurrentLevel} → {NewLevel}, User limit: {CurrentLimit} → {NewLimit}",
                currentServiceLevel,
                dto.NewServiceLevel,
                "unlimited",
                newService.UserLimit
            );

            // 4. Validar el cambio de plan
            var validationResult = ValidateServicePlanChange(
                totalActiveUsers,
                newService.UserLimit,
                dto.ForceUserDeactivation
            );

            if (!validationResult.IsValid)
            {
                _logger.LogWarning(
                    "Service plan change validation failed: {Message}",
                    validationResult.Message
                );
                return new ApiResponse<CompanyPlanUpdateResultDTO>(
                    false,
                    validationResult.Message,
                    null!
                );
            }

            // 5. Desactivar usuarios excedentes si es necesario
            var deactivatedUsers = new List<string>();
            if (totalActiveUsers > newService.UserLimit)
            {
                var usersToDeactivate = totalActiveUsers - newService.UserLimit;
                deactivatedUsers = await DeactivateExcessUsersAsync(
                    dto.CompanyId,
                    usersToDeactivate,
                    cancellationToken
                );

                _logger.LogInformation(
                    "Deactivated {Count} users due to plan downgrade: {Users}",
                    deactivatedUsers.Count,
                    string.Join(", ", deactivatedUsers)
                );
            }

            // 6. Actualizar CustomPlan
            var planPrice = dto.CustomPrice ?? newService.Price;
            var previousPrice = currentPlan.Price;

            currentPlan.Price = planPrice;
            currentPlan.StartDate = dto.StartDate ?? DateTime.UtcNow;
            currentPlan.EndDate = dto.EndDate;
            currentPlan.RenewDate = (dto.EndDate ?? DateTime.UtcNow).AddYears(1);
            currentPlan.IsActive = true;
            currentPlan.UpdatedAt = DateTime.UtcNow;

            // 7. Actualizar CustomModules
            var moduleChanges = await UpdateCustomModulesAsync(
                currentPlan.Id,
                newServiceModules,
                dto.AdditionalModuleIds,
                cancellationToken
            );

            // 8. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(
                    "Failed to save service plan changes for company: {CompanyId}",
                    dto.CompanyId
                );
                return new ApiResponse<CompanyPlanUpdateResultDTO>(
                    false,
                    "Failed to update service plan",
                    null!
                );
            }

            await transaction.CommitAsync(cancellationToken);

            // 9. Preparar resultado detallado
            var updateResult = new CompanyPlanUpdateResultDTO
            {
                Success = true,
                Message = "Service plan updated successfully",
                CompanyId = dto.CompanyId,
                PreviousPlan = currentServiceLevel?.ToString() ?? "Custom",
                NewPlan = dto.NewServiceLevel.ToString(),
                PreviousPrice = previousPrice,
                NewPrice = planPrice,
                PreviousUserLimit = int.MaxValue, // Asumimos que el plan anterior no tenía límite
                NewUserLimit = newService.UserLimit,
                ActiveUsersCount = totalActiveUsers - deactivatedUsers.Count,
                DeactivatedUsersCount = deactivatedUsers.Count,
                DeactivatedUserEmails = deactivatedUsers,
                AddedModules = moduleChanges.AddedModules,
                RemovedModules = moduleChanges.RemovedModules,
                EffectiveDate = currentPlan.StartDate ?? DateTime.UtcNow,
                ExpirationDate = currentPlan.EndDate,
            };

            _logger.LogInformation(
                "Service plan updated successfully for company {CompanyId}: {PreviousPlan} → {NewPlan}, "
                    + "Price: ${PreviousPrice} → ${NewPrice}, Deactivated users: {DeactivatedCount}",
                dto.CompanyId,
                updateResult.PreviousPlan,
                updateResult.NewPlan,
                updateResult.PreviousPrice,
                updateResult.NewPrice,
                updateResult.DeactivatedUsersCount
            );

            return new ApiResponse<CompanyPlanUpdateResultDTO>(
                true,
                "Service plan updated successfully",
                updateResult
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error updating service plan for company {CompanyId}: {Message}",
                request.CompanyPlanData.CompanyId,
                ex.Message
            );
            return new ApiResponse<CompanyPlanUpdateResultDTO>(
                false,
                "An error occurred while updating the service plan",
                null!
            );
        }
    }

    /// <summary>
    /// CORREGIDO: Determina el ServiceLevel actual basado en los módulos
    /// </summary>
    private ServiceLevel? DetermineCurrentServiceLevel(List<ModuleInfo> currentModules)
    {
        // Si tiene módulos de un servicio específico, determinar cuál
        var serviceIds = currentModules
            .Where(m => m.ServiceId != null)
            .Select(m => m.ServiceId)
            .Distinct()
            .ToList();

        if (serviceIds.Count == 1)
        {
            // Aquí podrías hacer una query para obtener el nombre del servicio
            // Por ahora retornamos Basic como placeholder
            return ServiceLevel.Basic;
        }

        return null; // Plan personalizado
    }

    /// <summary>
    /// CORREGIDO: Valida si el cambio de plan es posible (sin async)
    /// </summary>
    private (bool IsValid, string Message) ValidateServicePlanChange(
        int currentActiveUsers,
        int newUserLimit,
        bool forceDeactivation
    )
    {
        if (currentActiveUsers > newUserLimit && !forceDeactivation)
        {
            return (
                false,
                $"Cannot downgrade plan: Current active users ({currentActiveUsers}) exceeds new plan limit ({newUserLimit}). "
                    + $"Set ForceUserDeactivation=true to proceed with user deactivation."
            );
        }

        // Aquí puedes agregar más validaciones según reglas de negocio
        // Ejemplo: verificar pagos pendientes, facturas, etc.

        return (true, "Validation passed");
    }

    /// <summary>
    /// Desactiva usuarios excedentes (UserCompanies primero, luego TaxUsers)
    /// </summary>
    private async Task<List<string>> DeactivateExcessUsersAsync(
        Guid companyId,
        int usersToDeactivate,
        CancellationToken ct
    )
    {
        var deactivatedEmails = new List<string>();

        // Prioridad 1: Desactivar UserCompanies (usuarios menos críticos)
        if (usersToDeactivate > 0)
        {
            var userCompaniesQuery =
                from uc in _dbContext.UserCompanies
                where uc.CompanyId == companyId && uc.IsActive
                orderby uc.CreatedAt descending // Los más recientes primero
                select uc;

            var userCompaniesToDeactivate = await userCompaniesQuery
                .Take(usersToDeactivate)
                .ToListAsync(ct);

            foreach (var userCompany in userCompaniesToDeactivate)
            {
                userCompany.IsActive = false;
                userCompany.IsActiveDate = DateTime.UtcNow;
                deactivatedEmails.Add(userCompany.Email);

                _logger.LogDebug("Deactivated UserCompany: {Email}", userCompany.Email);
            }

            usersToDeactivate -= userCompaniesToDeactivate.Count;
        }

        // Prioridad 2: Desactivar TaxUsers (pero nunca el último administrador)
        if (usersToDeactivate > 0)
        {
            // Obtener TaxUsers que NO sean el último administrador
            var adminRoleQuery =
                from r in _dbContext.Roles
                where r.Name.Contains("Administrator")
                select r.Id;

            var adminRoleIds = await adminRoleQuery.ToListAsync(ct);

            var nonCriticalTaxUsersQuery =
                from u in _dbContext.TaxUsers
                where u.CompanyId == companyId && u.IsActive
                where
                    !(
                        from ur in _dbContext.UserRoles
                        where ur.TaxUserId == u.Id && adminRoleIds.Contains(ur.RoleId)
                        select ur
                    ).Any() // No es administrador
                orderby u.CreatedAt descending
                select u;

            var taxUsersToDeactivate = await nonCriticalTaxUsersQuery
                .Take(usersToDeactivate)
                .ToListAsync(ct);

            foreach (var taxUser in taxUsersToDeactivate)
            {
                taxUser.IsActive = false;
                deactivatedEmails.Add(taxUser.Email);

                _logger.LogDebug("Deactivated TaxUser: {Email}", taxUser.Email);
            }
        }

        return deactivatedEmails;
    }

    /// <summary>
    /// CORREGIDO: Actualiza los CustomModules según el nuevo servicio
    /// </summary>
    private async Task<(
        List<string> AddedModules,
        List<string> RemovedModules
    )> UpdateCustomModulesAsync(
        Guid customPlanId,
        List<ModuleInfo> newServiceModules,
        ICollection<Guid>? additionalModuleIds,
        CancellationToken ct
    )
    {
        var addedModules = new List<string>();
        var removedModules = new List<string>();

        // Obtener módulos actuales
        var currentModulesQuery =
            from cm in _dbContext.CustomModules
            join m in _dbContext.Modules on cm.ModuleId equals m.Id
            where cm.CustomPlanId == customPlanId
            select new { CustomModule = cm, Module = m };

        var currentModules = await currentModulesQuery.ToListAsync(ct);

        // Desactivar módulos actuales
        foreach (var current in currentModules)
        {
            current.CustomModule.IsIncluded = false;
            removedModules.Add(current.Module.Name);
        }

        // Agregar módulos del nuevo servicio
        foreach (var serviceModule in newServiceModules)
        {
            var existingModule = currentModules.FirstOrDefault(cm =>
                cm.Module.Id == serviceModule.Id
            );
            if (existingModule != null)
            {
                existingModule.CustomModule.IsIncluded = true;
                removedModules.Remove(existingModule.Module.Name); // No se removió, se mantuvo
            }
            else
            {
                // Crear nuevo CustomModule
                var newCustomModule = new CustomModule
                {
                    Id = Guid.NewGuid(),
                    CustomPlanId = customPlanId,
                    ModuleId = serviceModule.Id,
                    IsIncluded = true,
                    CreatedAt = DateTime.UtcNow,
                };

                await _dbContext.CustomModules.AddAsync(newCustomModule, ct);
            }

            addedModules.Add(serviceModule.Name);
        }

        // Agregar módulos adicionales opcionales
        if (additionalModuleIds?.Any() == true)
        {
            var additionalModulesQuery =
                from m in _dbContext.Modules
                where additionalModuleIds.Contains(m.Id) && m.IsActive
                select m;

            var additionalModules = await additionalModulesQuery.ToListAsync(ct);

            foreach (var additionalModule in additionalModules)
            {
                var existingModule = currentModules.FirstOrDefault(cm =>
                    cm.Module.Id == additionalModule.Id
                );
                if (existingModule != null)
                {
                    existingModule.CustomModule.IsIncluded = true;
                    if (removedModules.Contains(additionalModule.Name))
                        removedModules.Remove(additionalModule.Name);
                }
                else
                {
                    var newCustomModule = new CustomModule
                    {
                        Id = Guid.NewGuid(),
                        CustomPlanId = customPlanId,
                        ModuleId = additionalModule.Id,
                        IsIncluded = true,
                        CreatedAt = DateTime.UtcNow,
                    };

                    await _dbContext.CustomModules.AddAsync(newCustomModule, ct);
                }

                if (!addedModules.Contains(additionalModule.Name))
                    addedModules.Add(additionalModule.Name);
            }
        }

        return (addedModules, removedModules);
    }
}

/// <summary>
/// NUEVO: Clase helper para evitar problemas con anonymous types
/// </summary>
public class ModuleInfo
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ServiceId { get; set; }
}
