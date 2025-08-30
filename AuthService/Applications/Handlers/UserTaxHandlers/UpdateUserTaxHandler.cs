using Applications.DTOs.AddressDTOs;
using AuthService.Domains.Addresses;
using AuthService.Domains.Users;
using AuthService.Infraestructure.Services;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserTaxHandlers;

public class UpdateUserTaxHandler : IRequestHandler<UpdateTaxUserCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UpdateUserTaxHandler> _logger;
    private readonly IPasswordHash _passwordHash;

    private const int USA = 220;

    public UpdateUserTaxHandler(
        ApplicationDbContext dbContext,
        ILogger<UpdateUserTaxHandler> logger,
        IPasswordHash passwordHash
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _passwordHash = passwordHash;
    }

    public async Task<ApiResponse<bool>> Handle(
        UpdateTaxUserCommands request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Buscar el usuario con info de company (sin CustomPlan)
            var userQuery =
                from u in _dbContext.TaxUsers
                join c in _dbContext.Companies on u.CompanyId equals c.Id
                where u.Id == request.UserTax.Id
                select new { User = u, Company = c };

            var userData = await userQuery.FirstOrDefaultAsync(cancellationToken);
            if (userData?.User == null)
            {
                _logger.LogWarning("Tax user not found: {UserId}", request.UserTax.Id);
                return new ApiResponse<bool>(false, "Tax user not found", false);
            }

            var user = userData.User;
            var company = userData.Company;

            // Verificar si se está intentando desactivar al Owner
            if (user.IsOwner && request.UserTax.IsActive == false)
            {
                _logger.LogWarning("Cannot deactivate company owner: {UserId}", request.UserTax.Id);
                return new ApiResponse<bool>(false, "Cannot deactivate the company owner", false);
            }

            // Verificar email único
            if (!string.IsNullOrEmpty(request.UserTax.Email) && request.UserTax.Email != user.Email)
            {
                var emailExists = await _dbContext.TaxUsers.AnyAsync(
                    u => u.Email == request.UserTax.Email && u.Id != request.UserTax.Id,
                    cancellationToken
                );

                if (emailExists)
                {
                    _logger.LogWarning("Email already exists: {Email}", request.UserTax.Email);
                    return new ApiResponse<bool>(false, "Email already exists", false);
                }
            }

            // VALIDACIÓN DE ACTIVACIÓN SIMPLIFICADA
            // El frontend debe validar límites consultando SubscriptionsService
            if (!user.IsOwner && request.UserTax.IsActive == true && !user.IsActive)
            {
                var currentActiveUsersCount = await _dbContext.TaxUsers.CountAsync(
                    u => u.CompanyId == user.CompanyId && u.IsActive && u.Id != user.Id,
                    cancellationToken
                );

                _logger.LogInformation(
                    "Activating user for company {CompanyId} (ServiceLevel: {ServiceLevel}). Current active users: {CurrentUsers}",
                    user.CompanyId,
                    company.ServiceLevel,
                    currentActiveUsersCount
                );

                // El frontend debe haber validado límites antes de llamar este endpoint
                // Aquí solo logueamos para auditoría
            }

            // Guardar valores inmutables
            var currentPassword = user.Password;
            var currentIsOwner = user.IsOwner;
            var currentCompanyId = user.CompanyId;

            // MAPEO MANUAL SELECTIVO
            if (!string.IsNullOrEmpty(request.UserTax.Email))
            {
                user.Email = request.UserTax.Email;
                _logger.LogDebug(
                    "Updated email for user: {UserId} to {Email}",
                    user.Id,
                    user.Email
                );
            }

            if (request.UserTax.Name != null)
            {
                user.Name = string.IsNullOrWhiteSpace(request.UserTax.Name)
                    ? null
                    : request.UserTax.Name.Trim();
                _logger.LogDebug("Updated name for user: {UserId} to {Name}", user.Id, user.Name);
            }

            if (request.UserTax.LastName != null)
            {
                user.LastName = string.IsNullOrWhiteSpace(request.UserTax.LastName)
                    ? null
                    : request.UserTax.LastName.Trim();
                _logger.LogDebug(
                    "Updated last name for user: {UserId} to {LastName}",
                    user.Id,
                    user.LastName
                );
            }

            if (request.UserTax.PhoneNumber != null)
            {
                user.PhoneNumber = string.IsNullOrWhiteSpace(request.UserTax.PhoneNumber)
                    ? null
                    : request.UserTax.PhoneNumber.Trim();
                _logger.LogDebug(
                    "Updated phone for user: {UserId} to {Phone}",
                    user.Id,
                    user.PhoneNumber
                );
            }

            if (request.UserTax.IsActive.HasValue)
            {
                user.IsActive = request.UserTax.IsActive.Value;
                _logger.LogDebug(
                    "Updated active status for user: {UserId} to {IsActive}",
                    user.Id,
                    user.IsActive
                );
            }

            // Restaurar valores inmutables
            user.IsOwner = currentIsOwner;
            user.CompanyId = currentCompanyId;

            // Manejar contraseña
            if (!string.IsNullOrWhiteSpace(request.UserTax.Password))
            {
                user.Password = _passwordHash.HashPassword(request.UserTax.Password);
                _logger.LogDebug("Password updated for tax user: {UserId}", user.Id);
            }
            else
            {
                user.Password = currentPassword;
            }

            user.UpdatedAt = DateTime.UtcNow;

            // Manejar dirección
            await HandleAddressUpdateAsync(user, request.UserTax.Address, cancellationToken);

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Tax user updated successfully: UserId={UserId}, Company={CompanyName} (ServiceLevel: {ServiceLevel}), IsOwner={IsOwner}",
                    user.Id,
                    company.IsCompany ? company.CompanyName : company.FullName,
                    company.ServiceLevel,
                    user.IsOwner
                );
                return new ApiResponse<bool>(true, "Tax user updated successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to update tax user: {UserId}", request.UserTax.Id);
                return new ApiResponse<bool>(false, "Failed to update tax user", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error updating tax user: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    #region Address Management (sin cambios)

    private async Task HandleAddressUpdateAsync(
        TaxUser user,
        AddressDTO? addressDto,
        CancellationToken cancellationToken
    )
    {
        if (addressDto == null)
        {
            _logger.LogDebug(
                "No address data provided - keeping current address for user: {UserId}",
                user.Id
            );
            return;
        }

        var validateResult = await ValidateAddressAsync(
            addressDto.CountryId,
            addressDto.StateId,
            cancellationToken
        );
        if (!validateResult.Success)
        {
            throw new InvalidOperationException(validateResult.Message);
        }

        if (IsAddressEmpty(addressDto))
        {
            _logger.LogDebug(
                "Empty address provided - removing address for user: {UserId}",
                user.Id
            );

            if (user.AddressId.HasValue)
            {
                var addressToDelete = await _dbContext.Addresses.FirstOrDefaultAsync(
                    a => a.Id == user.AddressId.Value,
                    cancellationToken
                );

                if (addressToDelete != null)
                {
                    _dbContext.Addresses.Remove(addressToDelete);
                    _logger.LogDebug(
                        "Removed address: {AddressId} for user: {UserId}",
                        addressToDelete.Id,
                        user.Id
                    );
                }

                user.AddressId = null;
            }
            return;
        }

        if (user.AddressId.HasValue)
        {
            var existingAddress = await _dbContext.Addresses.FirstOrDefaultAsync(
                a => a.Id == user.AddressId.Value,
                cancellationToken
            );

            if (existingAddress != null)
            {
                UpdateAddressFields(existingAddress, addressDto);
                _logger.LogDebug(
                    "Updated existing address: {AddressId} for user: {UserId}",
                    existingAddress.Id,
                    user.Id
                );
            }
            else
            {
                await CreateNewAddressAndAssignAsync(user, addressDto, cancellationToken);
            }
        }
        else
        {
            await CreateNewAddressAndAssignAsync(user, addressDto, cancellationToken);
        }
    }

    private void UpdateAddressFields(Address address, AddressDTO addressDto)
    {
        address.CountryId = addressDto.CountryId;
        address.StateId = addressDto.StateId;
        address.City = string.IsNullOrWhiteSpace(addressDto.City) ? null : addressDto.City.Trim();
        address.Street = string.IsNullOrWhiteSpace(addressDto.Street)
            ? null
            : addressDto.Street.Trim();
        address.Line = string.IsNullOrWhiteSpace(addressDto.Line) ? null : addressDto.Line.Trim();
        address.ZipCode = string.IsNullOrWhiteSpace(addressDto.ZipCode)
            ? null
            : addressDto.ZipCode.Trim();
        address.UpdatedAt = DateTime.UtcNow;
    }

    private async Task CreateNewAddressAndAssignAsync(
        TaxUser user,
        AddressDTO addressDto,
        CancellationToken cancellationToken
    )
    {
        var newAddress = new Address
        {
            Id = Guid.NewGuid(),
            CountryId = addressDto.CountryId,
            StateId = addressDto.StateId,
            City = string.IsNullOrWhiteSpace(addressDto.City) ? null : addressDto.City.Trim(),
            Street = string.IsNullOrWhiteSpace(addressDto.Street) ? null : addressDto.Street.Trim(),
            Line = string.IsNullOrWhiteSpace(addressDto.Line) ? null : addressDto.Line.Trim(),
            ZipCode = string.IsNullOrWhiteSpace(addressDto.ZipCode)
                ? null
                : addressDto.ZipCode.Trim(),
            CreatedAt = DateTime.UtcNow,
        };

        await _dbContext.Addresses.AddAsync(newAddress, cancellationToken);
        user.AddressId = newAddress.Id;

        _logger.LogDebug(
            "Created new address: {AddressId} for user: {UserId}",
            newAddress.Id,
            user.Id
        );
    }

    private bool IsAddressEmpty(AddressDTO addressDto) => addressDto.StateId <= 0;

    private async Task<(bool Success, string Message)> ValidateAddressAsync(
        int countryId,
        int stateId,
        CancellationToken cancellationToken
    )
    {
        if (countryId != USA)
            return (false, "Only United States (CountryId = 220) is supported.");

        var country = await _dbContext.Countries.FirstOrDefaultAsync(
            c => c.Id == countryId,
            cancellationToken
        );
        if (country == null)
            return (false, $"CountryId '{countryId}' not found.");

        var state = await _dbContext.States.FirstOrDefaultAsync(
            s => s.Id == stateId && s.CountryId == countryId,
            cancellationToken
        );
        if (state == null)
            return (false, $"StateId '{stateId}' not found for CountryId '{countryId}'.");

        return (true, "Address validation passed");
    }

    #endregion
}
