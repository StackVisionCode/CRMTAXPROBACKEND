using Applications.DTOs.AddressDTOs;
using AuthService.Applications.Common;
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
            // Buscar company con información básica (SIN CustomPlan)
            var companyQuery =
                from c in _dbContext.Companies
                where c.Id == request.CompanyTax.Id
                select new
                {
                    Company = c,
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

            _logger.LogInformation(
                "Updating company: {CompanyId}, ServiceLevel: {ServiceLevel}, Active TaxUsers: {ActiveCount}/{TotalCount} (Owner: {OwnerCount})",
                company.Id,
                company.ServiceLevel,
                companyData.CurrentActiveTaxUserCount,
                companyData.TotalTaxUserCount,
                companyData.OwnerCount
            );

            // VALIDACIONES BÁSICAS
            var validationResult = await ValidateCompanyUpdateAsync(
                company,
                request.CompanyTax,
                companyData.OwnerCount,
                cancellationToken
            );

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Validation failed: {Message}", validationResult.Message);
                return new ApiResponse<bool>(false, validationResult.Message, false);
            }

            // Guardar valores que no deben cambiar via este endpoint
            var currentAddressId = company.AddressId;

            // Actualizar campos básicos
            _mapper.Map(request.CompanyTax, company);
            company.UpdatedAt = DateTime.UtcNow;
            company.AddressId = currentAddressId; // Restaurar

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

            // Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);

                _logger.LogInformation(
                    "Company updated successfully: CompanyId={CompanyId}, Domain={Domain}, ServiceLevel={ServiceLevel}",
                    company.Id,
                    company.Domain,
                    company.ServiceLevel
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

    #region Validation Methods

    private async Task<(bool IsValid, string Message)> ValidateCompanyUpdateAsync(
        AuthService.Domains.Companies.Company company,
        UpdateCompanyDTO updateDto,
        int ownerCount,
        CancellationToken cancellationToken
    )
    {
        // Verificar que hay al menos un Owner
        if (ownerCount == 0)
        {
            _logger.LogError("Company {CompanyId} has no Owner", company.Id);
            return (false, "Company must have at least one Owner");
        }

        // Verificar dominio único (si cambió)
        if (!string.IsNullOrEmpty(updateDto.Domain) && updateDto.Domain != company.Domain)
        {
            var domainExists = await _dbContext.Companies.AnyAsync(
                c => c.Domain == updateDto.Domain && c.Id != updateDto.Id,
                cancellationToken
            );

            if (domainExists)
            {
                _logger.LogWarning("Domain already exists: {Domain}", updateDto.Domain);
                return (false, "Domain already exists");
            }

            // Validar formato del dominio
            if (!IsValidDomainFormat(updateDto.Domain))
            {
                return (false, "Domain format is invalid. Use only letters, numbers, and hyphens.");
            }
        }

        // Validar ServiceLevel si se especifica
        if (updateDto.ServiceLevel.HasValue)
        {
            if (!Enum.IsDefined(typeof(ServiceLevel), updateDto.ServiceLevel.Value))
            {
                return (
                    false,
                    "Invalid ServiceLevel. Valid values: 1=Basic, 2=Standard, 3=Pro, 99=Developer"
                );
            }

            _logger.LogInformation(
                "ServiceLevel change: {CompanyId} from {OldLevel} to {NewLevel}",
                company.Id,
                company.ServiceLevel,
                updateDto.ServiceLevel.Value
            );
        }

        // Validar cambio de tipo de cuenta
        if (updateDto.IsCompany != company.IsCompany)
        {
            _logger.LogInformation(
                "Account type change: {CompanyId} from IsCompany={OldType} to IsCompany={NewType}",
                company.Id,
                company.IsCompany,
                updateDto.IsCompany
            );

            // El frontend debe validar límites de usuarios consultando SubscriptionsService
            // Aquí solo validamos que existe al menos un Owner activo
        }

        return (true, "Validation passed");
    }

    private static bool IsValidDomainFormat(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return false;

        return System.Text.RegularExpressions.Regex.IsMatch(domain, @"^[a-zA-Z0-9-]+$")
            && domain.Length >= 3
            && domain.Length <= 50;
    }

    #endregion

    #region Address Management

    private async Task<(bool Success, string Message)> HandleAddressUpdateAsync(
        AuthService.Domains.Companies.Company company,
        AddressDTO? addressDto,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (addressDto != null)
            {
                var validationResult = await ValidateAddressAsync(
                    addressDto.CountryId,
                    addressDto.StateId,
                    cancellationToken
                );

                if (!validationResult.Success)
                    return (false, validationResult.Message);

                if (company.AddressId.HasValue)
                {
                    // Actualizar dirección existente
                    var existingAddress = await _dbContext.Addresses.FirstOrDefaultAsync(
                        a => a.Id == company.AddressId.Value,
                        cancellationToken
                    );

                    if (existingAddress != null)
                    {
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
                    }
                    else
                    {
                        // Crear nueva dirección si la referenciada no existe
                        await CreateNewAddressAsync(company, addressDto, cancellationToken);
                    }
                }
                else
                {
                    // Crear nueva dirección
                    await CreateNewAddressAsync(company, addressDto, cancellationToken);
                }
            }
            else if (company.AddressId.HasValue)
            {
                // Remover referencia a dirección
                _logger.LogInformation(
                    "Removing address reference for company: {CompanyId}",
                    company.Id
                );
                company.AddressId = null;
            }

            return (true, "Address updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating address for company: {CompanyId}", company.Id);
            return (false, $"Failed to update address: {ex.Message}");
        }
    }

    private async Task CreateNewAddressAsync(
        AuthService.Domains.Companies.Company company,
        AddressDTO addressDto,
        CancellationToken cancellationToken
    )
    {
        var newAddress = new Address
        {
            Id = Guid.NewGuid(),
            CountryId = addressDto.CountryId,
            StateId = addressDto.StateId,
            City = addressDto.City?.Trim(),
            Street = addressDto.Street?.Trim(),
            Line = addressDto.Line?.Trim(),
            ZipCode = addressDto.ZipCode?.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        await _dbContext.Addresses.AddAsync(newAddress, cancellationToken);
        company.AddressId = newAddress.Id;

        _logger.LogDebug("Created new address: {AddressId}", newAddress.Id);
    }

    private async Task<(bool Success, string Message)> ValidateAddressAsync(
        int countryId,
        int stateId,
        CancellationToken cancellationToken
    )
    {
        if (countryId != USA)
            return (false, "Only United States (CountryId = 220) is supported.");

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
        ).FirstOrDefaultAsync(cancellationToken);

        if (addressValidation == null)
        {
            return (false, $"Invalid CountryId '{countryId}' or StateId '{stateId}'");
        }

        return (true, "Address validation passed");
    }

    #endregion
}
