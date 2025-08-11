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
            // ✅ MEJORADO: Buscar company con información completa del CustomPlan
            var companyQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == request.CompanyTax.Id
                select new
                {
                    Company = c,
                    CustomPlan = cp,
                    CurrentUserCount = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id)
                        + _dbContext.UserCompanies.Count(uc => uc.CompanyId == c.Id),
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
                "Updating company: {CompanyId}, CurrentUsers: {UserCount}, CustomPlanId: {CustomPlanId}",
                company.Id,
                companyData.CurrentUserCount,
                customPlan.Id
            );

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
                companyData.CurrentUserCount,
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

            // ✅ Guardar valores actuales para no sobreescribirlos
            var currentAddressId = company.AddressId;
            var currentCustomPlanId = company.CustomPlanId;

            // Actualizar campos básicos de la company
            _mapper.Map(request.CompanyTax, company);
            company.UpdatedAt = DateTime.UtcNow;

            // ✅ Restaurar valores críticos que no deben cambiar via este endpoint
            company.AddressId = currentAddressId;
            company.CustomPlanId = currentCustomPlanId;

            // ✅ MEJORADO: Manejar actualización de dirección
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

            // ✅ Guardar todos los cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Company updated successfully: CompanyId={CompanyId}, Domain={Domain}, AddressId={AddressId}",
                    company.Id,
                    company.Domain,
                    company.AddressId
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

    /// <summary>
    /// Valida cambios críticos que podrían afectar el plan actual
    /// </summary>
    private async Task<(bool IsValid, string Message)> ValidateCriticalChangesAsync(
        AuthService.Domains.Companies.Company company,
        UpdateCompanyDTO updateDto,
        int currentUserCount,
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

            // Si tiene usuarios activos, no permitir cambio de tipo de cuenta
            if (currentUserCount > 1) // Más que solo el admin
            {
                return (
                    false,
                    "Cannot change account type when company has multiple users. Remove additional users first."
                );
            }
        }

        // Validar límites del plan actual si es necesario
        // (Esta lógica se puede expandir según las reglas de negocio)

        return (true, "Validation passed");
    }

    /// <summary>
    /// ✅ MEJORADO: Maneja la actualización de dirección con mejor control
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
                    var addressQuery =
                        from a in _dbContext.Addresses
                        where a.Id == company.AddressId.Value
                        select a;

                    var existingAddress = await addressQuery.FirstOrDefaultAsync(ct);
                    if (existingAddress != null)
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
                // Eliminar dirección si se envía null
                var addressToDeleteQuery =
                    from a in _dbContext.Addresses
                    where a.Id == company.AddressId.Value
                    select a;

                var addressToDelete = await addressToDeleteQuery.FirstOrDefaultAsync(ct);
                if (addressToDelete != null)
                {
                    _dbContext.Addresses.Remove(addressToDelete);
                    _logger.LogDebug(
                        "Marked address for deletion: {AddressId}",
                        addressToDelete.Id
                    );
                }
                company.AddressId = null;
            }

            return (true, "Address updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating address for company: {CompanyId}", company.Id);
            return (false, "Failed to update address");
        }
    }

    /// <summary>
    /// Actualiza información del CustomPlan si es necesario
    /// </summary>
    private async Task<(bool Success, string Message)> UpdateCustomPlanInfoAsync(
        AuthService.Domains.CustomPlans.CustomPlan customPlan,
        AuthService.Domains.Companies.Company company,
        CancellationToken ct
    )
    {
        try
        {
            bool planNeedsUpdate = false;

            // Ejemplo: Si cambia el dominio, podríamos querer actualizar algo en el plan
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

            return (true, "CustomPlan updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating CustomPlan for company: {CompanyId}", company.Id);
            return (false, "Failed to update custom plan information");
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
    /// Valida que el país y estado existan
    /// </summary>
    private async Task<(bool Success, string Message)> ValidateAddressAsync(
        int countryId,
        int stateId,
        CancellationToken ct
    )
    {
        if (countryId != USA)
            return (false, "Only United States (CountryId = 220) is supported.");

        var countryQuery = from c in _dbContext.Countries where c.Id == countryId select c;
        var country = await countryQuery.FirstOrDefaultAsync(ct);
        if (country is null)
            return (false, $"CountryId '{countryId}' not found.");

        var stateQuery =
            from s in _dbContext.States
            where s.Id == stateId && s.CountryId == countryId
            select s;
        var state = await stateQuery.FirstOrDefaultAsync(ct);
        if (state is null)
            return (false, $"StateId '{stateId}' not found for CountryId '{countryId}'.");

        return (true, "Address validation passed");
    }
}
