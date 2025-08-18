using AuthService.Applications.Common;
using AuthService.Domains.Modules;
using AuthService.DTOs.CompanyDTOs;
using AuthService.DTOs.ModuleDTOs;
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

            // Solo TaxUsers ahora, no UserCompanies
            var currentStateQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == dto.CompanyId
                select new
                {
                    Company = c,
                    CustomPlan = cp,
                    // Solo contar TaxUsers activos
                    TaxUsersCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsActive
                    ),
                    OwnerCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsOwner && u.IsActive
                    ),
                    RegularUsersCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && !u.IsOwner && u.IsActive
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

            // Obtener módulos actuales por separado
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
            var totalActiveUsers = currentState.TaxUsersCount;

            _logger.LogInformation(
                "Starting service plan update for company {CompanyId}: TaxUsers: {TaxUsers} (Owner: {Owner}, Regular: {Regular})",
                dto.CompanyId,
                totalActiveUsers,
                currentState.OwnerCount,
                currentState.RegularUsersCount
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

            // Obtener módulos del nuevo servicio por separado
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
            var currentServiceLevel = DetermineCurrentServiceLevel(
                currentModules,
                cancellationToken
            );

            _logger.LogInformation(
                "Plan change: {CurrentLevel} → {NewLevel}, User limit: unlimited → {NewLimit}",
                currentServiceLevel,
                dto.NewServiceLevel,
                newService.UserLimit
            );

            // Validar el cambio de plan considerando solo TaxUsers
            var validationResult = ValidateServicePlanChange(
                totalActiveUsers,
                newService.UserLimit,
                currentState.OwnerCount,
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

            // Desactivar TaxUsers excedentes si es necesario
            var deactivatedUsers = new List<string>();
            if (totalActiveUsers > newService.UserLimit)
            {
                deactivatedUsers = await DeactivateExcessTaxUsersAsync(
                    dto.CompanyId,
                    totalActiveUsers, // 🔧 CAMBIO: Pasar totalActiveUsers
                    newService.UserLimit, // 🔧 CAMBIO: Pasar newUserLimit
                    cancellationToken
                );

                _logger.LogInformation(
                    "Deactivated {Count} TaxUsers due to plan downgrade: {Users}",
                    deactivatedUsers.Count,
                    string.Join(", ", deactivatedUsers)
                );
            }

            // 6. Actualizar CustomPlan
            var planPrice = dto.CustomPrice ?? newService.Price;
            var planUserLimit = dto.CustomUserLimit ?? newService.UserLimit;
            var previousPrice = currentPlan.Price;
            var previousUserLimit = currentPlan.UserLimit;

            currentPlan.Price = planPrice;
            currentPlan.UserLimit = planUserLimit;
            currentPlan.StartDate = dto.StartDate ?? DateTime.UtcNow;
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
                PreviousUserLimit = previousUserLimit,
                NewUserLimit = planUserLimit,
                ActiveUsersCount = totalActiveUsers - deactivatedUsers.Count,
                DeactivatedUsersCount = deactivatedUsers.Count,
                DeactivatedUserEmails = deactivatedUsers,
                AddedModules = moduleChanges.AddedModules,
                RemovedModules = moduleChanges.RemovedModules,
                EffectiveDate = currentPlan.StartDate ?? DateTime.UtcNow,
                ExpirationDate =
                    !currentPlan.isRenewed && currentPlan.RenewDate > DateTime.UtcNow
                        ? currentPlan.RenewDate
                        : null,
            };
            _logger.LogInformation(
                "Service plan updated successfully for company {CompanyId}: {PreviousPlan} → {NewPlan}, "
                    + "Price: ${PreviousPrice} → ${NewPrice}, Deactivated TaxUsers: {DeactivatedCount}",
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
    /// Determina el ServiceLevel actual basado en los módulos
    /// </summary>
    private async Task<ServiceLevel?> DetermineCurrentServiceLevel(
        List<ModuleInfo> currentModules,
        CancellationToken ct
    )
    {
        var serviceIds = currentModules
            .Where(m => m.ServiceId.HasValue)
            .Select(m => m.ServiceId ?? Guid.Empty)
            .Distinct()
            .ToList();

        if (serviceIds.Count == 1)
        {
            // 🔧 MEJORA: Query real para obtener el ServiceLevel
            var serviceQuery = await (
                from s in _dbContext.Services
                where s.Id == serviceIds.First()
                select s.Name
            ).FirstOrDefaultAsync(ct);

            return serviceQuery switch
            {
                "Basic" => ServiceLevel.Basic,
                "Standard" => ServiceLevel.Standard,
                "Pro" => ServiceLevel.Pro,
                _ => null,
            };
        }

        return null; // Plan personalizado con múltiples servicios
    }

    /// <summary>
    /// Valida si el cambio de plan es posible
    /// </summary>
    private (bool IsValid, string Message) ValidateServicePlanChange(
        int currentActiveTaxUsers,
        int newUserLimit,
        int ownerCount,
        bool forceDeactivation
    )
    {
        // Verificar que siempre hay al menos un Owner
        if (ownerCount == 0)
        {
            return (
                false,
                "Company must have at least one Owner. Cannot proceed with plan change."
            );
        }

        // Owner siempre cuenta en el límite
        var regularUsersCount = currentActiveTaxUsers - ownerCount;

        // Verificar contra newUserLimit directamente
        if (currentActiveTaxUsers > newUserLimit && !forceDeactivation)
        {
            return (
                false,
                $"Cannot downgrade plan: Current active users ({currentActiveTaxUsers}) exceed new plan limit ({newUserLimit}). "
                    + $"Set ForceUserDeactivation=true to proceed."
            );
        }

        // Verificar que el nuevo límite de usuarios es válido
        if (newUserLimit < 1)
        {
            return (false, "Service plan must allow at least 1 user (the Owner).");
        }

        return (true, "Validation passed");
    }

    /// <summary>
    /// Desactiva TaxUsers excedentes (nunca el Owner)
    /// </summary>
    private async Task<List<string>> DeactivateExcessTaxUsersAsync(
        Guid companyId,
        int totalActiveUsers,
        int newUserLimit,
        CancellationToken ct
    )
    {
        var deactivatedEmails = new List<string>();

        // newUserLimit incluye al Owner, así que necesitamos desactivar el exceso
        var usersToDeactivate = totalActiveUsers - newUserLimit;

        if (usersToDeactivate <= 0)
            return deactivatedEmails;

        // Solo desactivar TaxUsers que NO sean Owner
        var nonOwnerTaxUsersQuery =
            from u in _dbContext.TaxUsers
            where u.CompanyId == companyId && u.IsActive && !u.IsOwner
            orderby u.CreatedAt descending
            select u;

        var taxUsersToDeactivate = await nonOwnerTaxUsersQuery
            .Take(usersToDeactivate)
            .ToListAsync(ct);

        foreach (var taxUser in taxUsersToDeactivate)
        {
            taxUser.IsActive = false;
            taxUser.UpdatedAt = DateTime.UtcNow;
            deactivatedEmails.Add(taxUser.Email);

            _logger.LogDebug("Deactivated TaxUser: {Email}", taxUser.Email);
        }

        var remainingToDeactivate = usersToDeactivate - taxUsersToDeactivate.Count;
        if (remainingToDeactivate > 0)
        {
            _logger.LogWarning(
                "Could only deactivate {Deactivated} of {Required} regular users. "
                    + "{Remaining} users remain above limit (Owner cannot be deactivated). "
                    + "Total active users will be {FinalCount} (limit: {Limit})",
                taxUsersToDeactivate.Count,
                usersToDeactivate,
                remainingToDeactivate,
                totalActiveUsers - taxUsersToDeactivate.Count,
                newUserLimit
            );
        }

        return deactivatedEmails;
    }

    /// <summary>
    /// Actualiza los CustomModules según el nuevo servicio
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
                removedModules.Remove(existingModule.Module.Name);
            }
            else
            {
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
