using Applications.DTOs.CompanyDTOs;
using AuthService.Applications.DTOs.CompanyDTOs;
using AuthService.Domains.Addresses;
using AutoMapper;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.CompanyHandlers;

public class UpdateCompanyTaxHandler : IRequestHandler<UpdateTaxCompanyCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UpdateCompanyTaxHandler> _logger;
    private readonly IMapper _mapper;

    private const int USA = 220;

    public UpdateCompanyTaxHandler(
        ApplicationDbContext dbContext,
        ILogger<UpdateCompanyTaxHandler> logger,
        IMapper mapper
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _mapper = mapper;
    }

    public async Task<ApiResponse<bool>> Handle(
        UpdateTaxCompanyCommands request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Buscar company con información completa del CustomPlan
            var companyQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == request.CompanyTax.Id
                select new
                {
                    Company = c,
                    CustomPlan = cp,
                    CurrentActiveTaxUserCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsActive
                    ),
                    TotalTaxUserCount = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id),
                    OwnerCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsOwner && u.IsActive
                    ),
                    HasAddress = c.AddressId.HasValue,
                    AddressId = c.AddressId,
                };

            var companyData = await companyQuery.FirstOrDefaultAsync(cancellationToken);
            if (companyData?.Company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", request.CompanyTax.Id);
                return new ApiResponse<bool>(false, "Company not found", false);
            }

            var company = companyData.Company;
            var customPlan = companyData.CustomPlan;

            _logger.LogInformation(
                "Updating company: {CompanyId}, Active TaxUsers: {ActiveCount}/{TotalCount} "
                    + "(Owner: {OwnerCount}), CustomPlanId: {CustomPlanId}",
                company.Id,
                companyData.CurrentActiveTaxUserCount,
                companyData.TotalTaxUserCount,
                companyData.OwnerCount,
                customPlan.Id
            );

            // Verificar que hay al menos un Owner
            if (companyData.OwnerCount == 0)
            {
                _logger.LogError("Company {CompanyId} has no Owner", company.Id);
                return new ApiResponse<bool>(false, "Company must have at least one Owner", false);
            }

            // Verificar si el dominio ya existe en otra company (si se está cambiando)
            if (
                !string.IsNullOrEmpty(request.CompanyTax.Domain)
                && request.CompanyTax.Domain != company.Domain
            )
            {
                var domainExistsQuery =
                    from c in _dbContext.Companies
                    where c.Domain == request.CompanyTax.Domain && c.Id != request.CompanyTax.Id
                    select c.Id;

                if (await domainExistsQuery.AnyAsync(cancellationToken))
                {
                    _logger.LogWarning(
                        "Domain already exists: {Domain}",
                        request.CompanyTax.Domain
                    );
                    return new ApiResponse<bool>(false, "Domain already exists", false);
                }
            }

            // Validar cambios críticos que podrían requerir upgrade de plan
            var criticalChanges = await ValidateCriticalChangesAsync(
                company,
                request.CompanyTax,
                companyData.CurrentActiveTaxUserCount,
                cancellationToken
            );

            if (!criticalChanges.IsValid)
            {
                _logger.LogWarning(
                    "Critical validation failed: {Message}",
                    criticalChanges.Message
                );
                return new ApiResponse<bool>(false, criticalChanges.Message, false);
            }

            // Guardar valores actuales para no sobreescribirlos
            var currentAddressId = company.AddressId;
            var currentCustomPlanId = company.CustomPlanId;

            // Actualizar campos básicos de la company
            _mapper.Map(request.CompanyTax, company);
            company.UpdatedAt = DateTime.UtcNow;

            // Restaurar valores críticos que no deben cambiar via este endpoint
            company.AddressId = currentAddressId;
            company.CustomPlanId = currentCustomPlanId;

            // Manejar actualización de dirección
            var addressResult = await HandleAddressUpdateAsync(
                company,
                request.CompanyTax.Address,
                cancellationToken
            );

            if (!addressResult.Success)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<bool>(false, addressResult.Message, false);
            }

            // Actualizar información relacionada del CustomPlan si es necesario
            var planUpdateResult = await UpdateCustomPlanInfoAsync(
                customPlan,
                company,
                cancellationToken
            );

            if (!planUpdateResult.Success)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<bool>(false, planUpdateResult.Message, false);
            }

            // Guardar todos los cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);

                // Log con más detalles
                _logger.LogInformation(
                    "Company updated successfully: CompanyId={CompanyId}, "
                        + "Domain={Domain}, IsCompany={IsCompany}, "
                        + "AddressId={AddressId}, CustomPlanId={CustomPlanId}",
                    company.Id,
                    company.Domain,
                    company.IsCompany,
                    company.AddressId,
                    company.CustomPlanId
                );

                return new ApiResponse<bool>(true, "Company updated successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to save company changes: {CompanyId}", company.Id);
                return new ApiResponse<bool>(false, "Failed to update company", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error updating company {CompanyId}: {Message}",
                request.CompanyTax.Id,
                ex.Message
            );
            return new ApiResponse<bool>(
                false,
                "An error occurred while updating the company",
                false
            );
        }
    }

    #region Helper Methods

    /// <summary>
    /// Valida cambios críticos que podrían afectar el plan actual
    /// </summary>
    private async Task<(bool IsValid, string Message)> ValidateCriticalChangesAsync(
        AuthService.Domains.Companies.Company company,
        UpdateCompanyDTO updateDto,
        int currentActiveTaxUserCount,
        CancellationToken ct
    )
    {
        // Validar si el cambio de tipo de cuenta requiere validaciones adicionales
        if (updateDto.IsCompany != company.IsCompany)
        {
            _logger.LogInformation(
                "Account type change detected: {CompanyId} from IsCompany={OldType} to IsCompany={NewType}",
                company.Id,
                company.IsCompany,
                updateDto.IsCompany
            );

            // 🔧 MEJORA: Solo verificar usuarios activos
            if (currentActiveTaxUserCount > 1) // Más que solo el Owner
            {
                return (
                    false,
                    $"Cannot change account type when company has {currentActiveTaxUserCount} active users. "
                        + "Deactivate additional users first."
                );
            }

            // Verificar que el Owner está activo
            var activeOwnerExists = await _dbContext.TaxUsers.AnyAsync(
                u => u.CompanyId == company.Id && u.IsOwner && u.IsActive,
                ct
            );

            if (!activeOwnerExists)
            {
                return (false, "Cannot change account type: No active Owner found.");
            }
        }

        // Validar cambios de dominio con mejor mensaje
        if (
            !string.IsNullOrEmpty(updateDto.Domain)
            && !string.Equals(updateDto.Domain, company.Domain, StringComparison.OrdinalIgnoreCase)
        )
        {
            // Verificar formato del dominio
            if (!IsValidDomainFormat(updateDto.Domain))
            {
                return (false, "Domain format is invalid. Use only letters, numbers, and hyphens.");
            }

            _logger.LogInformation(
                "Domain change: {CompanyId} from '{OldDomain}' to '{NewDomain}'",
                company.Id,
                company.Domain,
                updateDto.Domain
            );
        }

        // Validar cambios de nombre de empresa
        if (
            updateDto.IsCompany
            && !string.IsNullOrEmpty(updateDto.CompanyName)
            && !string.Equals(
                updateDto.CompanyName,
                company.CompanyName,
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            _logger.LogInformation(
                "Company name change: {CompanyId} from '{OldName}' to '{NewName}'",
                company.Id,
                company.CompanyName,
                updateDto.CompanyName
            );

            // Aquí podrías agregar validaciones adicionales según reglas de negocio
            // Por ejemplo: verificar si requiere aprobación, documentos, etc.
        }

        return (true, "Validation passed");
    }

    private static bool IsValidDomainFormat(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return false;

        // Básico: solo letras, números y guiones, sin espacios
        return System.Text.RegularExpressions.Regex.IsMatch(domain, @"^[a-zA-Z0-9-]+$")
            && domain.Length >= 3
            && domain.Length <= 50;
    }

    /// <summary>
    /// Maneja la actualización de dirección con mejor control
    /// </summary>
    private async Task<(bool Success, string Message)> HandleAddressUpdateAsync(
        AuthService.Domains.Companies.Company company,
        AddressDTO? addressDto,
        CancellationToken ct
    )
    {
        try
        {
            if (addressDto is not null)
            {
                // Validar dirección
                var validateResult = await ValidateAddressAsync(
                    addressDto.CountryId,
                    addressDto.StateId,
                    ct
                );
                if (!validateResult.Success)
                    return (false, validateResult.Message);

                if (company.AddressId.HasValue)
                {
                    // Actualizar dirección existente
                    var existingAddress = await _dbContext.Addresses.FirstOrDefaultAsync(
                        a => a.Id == company.AddressId.Value,
                        ct
                    );

                    if (existingAddress != null)
                    {
                        // Verificar si realmente hay cambios
                        bool hasChanges =
                            existingAddress.CountryId != addressDto.CountryId
                            || existingAddress.StateId != addressDto.StateId
                            || existingAddress.City?.Trim() != addressDto.City?.Trim()
                            || existingAddress.Street?.Trim() != addressDto.Street?.Trim()
                            || existingAddress.Line?.Trim() != addressDto.Line?.Trim()
                            || existingAddress.ZipCode?.Trim() != addressDto.ZipCode?.Trim();

                        if (hasChanges)
                        {
                            existingAddress.CountryId = addressDto.CountryId;
                            existingAddress.StateId = addressDto.StateId;
                            existingAddress.City = addressDto.City?.Trim();
                            existingAddress.Street = addressDto.Street?.Trim();
                            existingAddress.Line = addressDto.Line?.Trim();
                            existingAddress.ZipCode = addressDto.ZipCode?.Trim();
                            existingAddress.UpdatedAt = DateTime.UtcNow;

                            _logger.LogDebug(
                                "Updated existing address: {AddressId}",
                                existingAddress.Id
                            );
                        }
                        else
                        {
                            _logger.LogDebug(
                                "No address changes detected for: {AddressId}",
                                existingAddress.Id
                            );
                        }
                    }
                    else
                    {
                        // Crear nueva dirección si la referenciada no existe
                        await CreateNewAddressAsync(company, addressDto, ct);
                    }
                }
                else
                {
                    // Crear nueva dirección
                    await CreateNewAddressAsync(company, addressDto, ct);
                }
            }
            else if (company.AddressId.HasValue)
            {
                // Soft delete o marcar como eliminado
                _logger.LogInformation(
                    "Removing address reference for company: {CompanyId}, AddressId: {AddressId}",
                    company.Id,
                    company.AddressId.Value
                );

                company.AddressId = null;
                // Nota: No eliminamos físicamente la Address por integridad referencial
            }

            return (true, "Address updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating address for company: {CompanyId}", company.Id);
            return (false, $"Failed to update address: {ex.Message}");
        }
    }

    /// <summary>
    /// Actualiza información del CustomPlan si es necesario
    /// </summary>
    private Task<(bool Success, string Message)> UpdateCustomPlanInfoAsync(
        AuthService.Domains.CustomPlans.CustomPlan customPlan,
        AuthService.Domains.Companies.Company company,
        CancellationToken ct
    )
    {
        try
        {
            bool planNeedsUpdate = false;

            // Si cambia el dominio, podríamos querer actualizar algo en el plan
            // Por ahora, solo actualizamos la fecha de modificación si hubo cambios
            if (company.UpdatedAt.HasValue)
            {
                // El CustomPlan no tiene UpdatedAt, pero podríamos agregar lógica aquí
                // Por ejemplo, si implementamos auditoría de cambios en el plan
                planNeedsUpdate = true;
            }

            if (planNeedsUpdate)
            {
                // Aquí podríamos agregar lógica específica de actualización del plan
                _logger.LogDebug("CustomPlan info updated for company: {CompanyId}", company.Id);
            }

            return Task.FromResult((true, "CustomPlan updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating CustomPlan for company: {CompanyId}", company.Id);
            return Task.FromResult((false, "Failed to update custom plan information"));
        }
    }

    /// <summary>
    /// Crea una nueva dirección y la asigna a la company
    /// </summary>
    private async Task CreateNewAddressAsync(
        AuthService.Domains.Companies.Company company,
        AddressDTO addrDto,
        CancellationToken ct
    )
    {
        var newAddress = new Address
        {
            Id = Guid.NewGuid(),
            CountryId = addrDto.CountryId,
            StateId = addrDto.StateId,
            City = addrDto.City?.Trim(),
            Street = addrDto.Street?.Trim(),
            Line = addrDto.Line?.Trim(),
            ZipCode = addrDto.ZipCode?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        await _dbContext.Addresses.AddAsync(newAddress, ct);
        company.AddressId = newAddress.Id;

        _logger.LogDebug("Created new address: {AddressId}", newAddress.Id);
    }

    /// <summary>
    /// Valida que el país y estado existan (solo USA por ahora)
    /// </summary>
    private async Task<(bool Success, string Message)> ValidateAddressAsync(
        int countryId,
        int stateId,
        CancellationToken ct
    )
    {
        // Solo Estados Unidos por ahora
        const int USA = 220;

        if (countryId != USA)
            return (false, "Only United States (CountryId = 220) is supported.");

        // Validar país y estado en una consulta
        var addressValidation = await (
            from c in _dbContext.Countries
            join s in _dbContext.States on c.Id equals s.CountryId
            where c.Id == countryId && s.Id == stateId
            select new
            {
                CountryName = c.Name,
                StateName = s.Name,
                IsUSA = c.Id == USA,
            }
        ).FirstOrDefaultAsync(ct);

        if (addressValidation == null)
        {
            return (false, $"Invalid CountryId '{countryId}' or StateId '{stateId}'");
        }

        if (!addressValidation.IsUSA)
        {
            return (false, "Only United States addresses are currently supported");
        }

        return (true, "Address validation passed");
    }

    #endregion
}
