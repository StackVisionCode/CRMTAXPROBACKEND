using Applications.DTOs.CompanyDTOs; // ✅ Agregar este using
using AuthService.Domains.Addresses;
using AutoMapper;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserTaxHandlers;

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
            // ✅ Buscar la compañía usando Join
            var companyQuery =
                from c in _dbContext.Companies
                where c.Id == request.CompanyTax.Id
                select c;

            var company = await companyQuery.FirstOrDefaultAsync(cancellationToken);
            if (company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", request.CompanyTax.Id);
                return new ApiResponse<bool>(false, "Company not found", false);
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

            // ✅ Guardar AddressId actual para evitar que AutoMapper lo sobreescriba
            var currentAddressId = company.AddressId;

            // Actualizar campos de la company
            _mapper.Map(request.CompanyTax, company);
            company.UpdatedAt = DateTime.UtcNow;

            // ✅ Restaurar AddressId (lo manejaremos manualmente)
            company.AddressId = currentAddressId;

            // ✅ MEJORADO: Manejar actualización de dirección con mejor logging
            if (request.CompanyTax.Address is not null)
            {
                var addrDto = request.CompanyTax.Address;
                var validateResult = await ValidateAddressAsync(
                    addrDto.CountryId,
                    addrDto.StateId,
                    cancellationToken
                );
                if (!validateResult.Success)
                    return new ApiResponse<bool>(false, validateResult.Message, false);

                if (company.AddressId.HasValue)
                {
                    // ✅ Actualizar dirección existente usando Join
                    var addressQuery =
                        from a in _dbContext.Addresses
                        where a.Id == company.AddressId.Value
                        select a;

                    var existingAddress = await addressQuery.FirstOrDefaultAsync(cancellationToken);
                    if (existingAddress != null)
                    {
                        existingAddress.CountryId = addrDto.CountryId;
                        existingAddress.StateId = addrDto.StateId;
                        existingAddress.City = addrDto.City?.Trim();
                        existingAddress.Street = addrDto.Street?.Trim();
                        existingAddress.Line = addrDto.Line?.Trim();
                        existingAddress.ZipCode = addrDto.ZipCode?.Trim();
                        existingAddress.UpdatedAt = DateTime.UtcNow;

                        _logger.LogDebug(
                            "Updated existing address: {AddressId} for company: {CompanyId}",
                            existingAddress.Id,
                            company.Id
                        );
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Address not found: {AddressId} for company: {CompanyId}",
                            company.AddressId.Value,
                            company.Id
                        );
                        // Crear nueva dirección si la referenciada no existe
                        await CreateNewAddressAsync(company, addrDto, cancellationToken);
                    }
                }
                else
                {
                    // ✅ Crear nueva dirección
                    await CreateNewAddressAsync(company, addrDto, cancellationToken);
                }
            }
            else if (company.AddressId.HasValue)
            {
                // ✅ Si no se envía dirección pero existía una, eliminarla
                var addressToDeleteQuery =
                    from a in _dbContext.Addresses
                    where a.Id == company.AddressId.Value
                    select a;

                var addressToDelete = await addressToDeleteQuery.FirstOrDefaultAsync(
                    cancellationToken
                );
                if (addressToDelete != null)
                {
                    _dbContext.Addresses.Remove(addressToDelete);
                    _logger.LogDebug(
                        "Removed address: {AddressId} for company: {CompanyId}",
                        addressToDelete.Id,
                        company.Id
                    );
                }
                company.AddressId = null;
            }

            // ✅ Guardar todos los cambios de una vez (esto evita problemas de FK)
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Company updated successfully: CompanyId={CompanyId}, AddressId={AddressId}",
                    company.Id,
                    company.AddressId
                );
                return new ApiResponse<bool>(true, "Company updated successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to update company: {CompanyId}", request.CompanyTax.Id);
                return new ApiResponse<bool>(false, "Failed to update company", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error updating company: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    /// <summary>
    /// Crea una nueva dirección y la asigna a la company
    /// </summary>
    private async Task CreateNewAddressAsync(
        AuthService.Domains.Companies.Company company,
        AddressDTO addrDto,
        CancellationToken cancellationToken
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

        await _dbContext.Addresses.AddAsync(newAddress, cancellationToken);
        company.AddressId = newAddress.Id;

        _logger.LogDebug(
            "Created new address: {AddressId} for company: {CompanyId}",
            newAddress.Id,
            company.Id
        );
    }

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

        return (true, "OK");
    }
}
