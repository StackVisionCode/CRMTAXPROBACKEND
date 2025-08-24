using Applications.DTOs.CompanyDTOs;
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
            // Buscar el usuario con info de company y custom plan para validaciones
            var userQuery =
                from u in _dbContext.TaxUsers
                join c in _dbContext.Companies on u.CompanyId equals c.Id
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                join s in _dbContext.Services on cp.Id equals s.Id into services
                from s in services.DefaultIfEmpty()
                where u.Id == request.UserTax.Id
                select new
                {
                    User = u,
                    Company = c,
                    CustomPlan = cp,
                    Service = s,
                };

            var userData = await userQuery.FirstOrDefaultAsync(cancellationToken);
            if (userData?.User == null)
            {
                _logger.LogWarning("Tax user not found: {UserId}", request.UserTax.Id);
                return new ApiResponse<bool>(false, "Tax user not found", false);
            }

            var user = userData.User;

            // Verificar si se está intentando desactivar al Owner
            if (user.IsOwner && request.UserTax.IsActive == false)
            {
                _logger.LogWarning("Cannot deactivate company owner: {UserId}", request.UserTax.Id);
                return new ApiResponse<bool>(false, "Cannot deactivate the company owner", false);
            }

            // Verificar si el email ya existe en otro usuario (si se está cambiando)
            if (!string.IsNullOrEmpty(request.UserTax.Email) && request.UserTax.Email != user.Email)
            {
                var emailExistsQuery =
                    from u in _dbContext.TaxUsers
                    where u.Email == request.UserTax.Email && u.Id != request.UserTax.Id
                    select u.Id;

                if (await emailExistsQuery.AnyAsync(cancellationToken))
                {
                    _logger.LogWarning("Email already exists: {Email}", request.UserTax.Email);
                    return new ApiResponse<bool>(false, "Email already exists", false);
                }
            }

            // Si se activa un usuario regular, verificar límites del plan
            if (!user.IsOwner && request.UserTax.IsActive == true && !user.IsActive)
            {
                var currentActiveUsersCount = await _dbContext.TaxUsers.CountAsync(
                    u => u.CompanyId == user.CompanyId && u.IsActive && u.Id != user.Id,
                    cancellationToken
                );

                if (currentActiveUsersCount >= userData.CustomPlan.UserLimit)
                {
                    _logger.LogWarning(
                        "Cannot activate user - plan limit exceeded: ActiveUsers={ActiveUsers}, Limit={Limit}, Company={CompanyId}",
                        currentActiveUsersCount,
                        userData.CustomPlan.UserLimit,
                        user.CompanyId
                    );
                    return new ApiResponse<bool>(
                        false,
                        $"Cannot activate user. Plan limit reached ({currentActiveUsersCount}/{userData.CustomPlan.UserLimit} users).",
                        false
                    );
                }
            }

            // 🔧 GUARDAR VALORES INMUTABLES AL INICIO
            var currentPassword = user.Password;
            var currentIsOwner = user.IsOwner;
            var currentCompanyId = user.CompanyId;

            // ⭐ MAPEO MANUAL SELECTIVO ⭐

            // Email - SOLO si viene en el DTO y no es nulo/vacío
            if (!string.IsNullOrEmpty(request.UserTax.Email))
            {
                user.Email = request.UserTax.Email;
                _logger.LogDebug(
                    "Updated email for user: {UserId} to {Email}",
                    user.Id,
                    user.Email
                );
            }

            // Nombre - Solo si viene en el DTO (permitir vacío pero no null)
            if (request.UserTax.Name != null)
            {
                user.Name = string.IsNullOrWhiteSpace(request.UserTax.Name)
                    ? null
                    : request.UserTax.Name.Trim();
                _logger.LogDebug("Updated name for user: {UserId} to {Name}", user.Id, user.Name);
            }

            // Apellido - Solo si viene en el DTO (permitir vacío pero no null)
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

            // Teléfono - Solo si viene en el DTO (permitir vacío pero no null)
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

            // Estado activo - Solo si viene en el DTO
            if (request.UserTax.IsActive.HasValue)
            {
                user.IsActive = request.UserTax.IsActive.Value;
                _logger.LogDebug(
                    "Updated active status for user: {UserId} to {IsActive}",
                    user.Id,
                    user.IsActive
                );
            }

            // RESTAURAR VALORES INMUTABLES (sin AddressId)
            user.IsOwner = currentIsOwner;
            user.CompanyId = currentCompanyId;

            // Manejar contraseña - Solo si viene en el DTO
            if (!string.IsNullOrWhiteSpace(request.UserTax.Password))
            {
                user.Password = _passwordHash.HashPassword(request.UserTax.Password);
                _logger.LogDebug("Password updated for tax user: {UserId}", user.Id);
            }
            else
            {
                user.Password = currentPassword; // Mantener contraseña actual
            }

            user.UpdatedAt = DateTime.UtcNow;

            // MANEJAR DIRECCIÓN
            await HandleAddressUpdateAsync(user, request.UserTax.Address, cancellationToken);

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Tax user updated successfully: UserId={UserId}, Company={CompanyName}, IsOwner={IsOwner}",
                    user.Id,
                    userData.Company.IsCompany
                        ? userData.Company.CompanyName
                        : userData.Company.FullName,
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

    /// <summary>
    /// Maneja la actualización de dirección sin perder referencia
    /// </summary>
    private async Task HandleAddressUpdateAsync(
        TaxUser user,
        AddressDTO? addressDto,
        CancellationToken cancellationToken
    )
    {
        // Caso 1: No se envía información de dirección - NO TOCAR NADA
        if (addressDto == null)
        {
            _logger.LogDebug(
                "No address data provided - keeping current address for user: {UserId}",
                user.Id
            );
            return; // Mantener dirección actual tal como está
        }

        // Validar dirección si se proporciona
        var validateResult = await ValidateAddressAsync(
            addressDto.CountryId,
            addressDto.StateId,
            cancellationToken
        );
        if (!validateResult.Success)
        {
            throw new InvalidOperationException(validateResult.Message);
        }

        // Caso 2: Se envía dirección vacía - ELIMINAR dirección existente
        if (IsAddressEmpty(addressDto))
        {
            _logger.LogDebug(
                "Empty address provided - removing address for user: {UserId}",
                user.Id
            );

            if (user.AddressId.HasValue)
            {
                // Buscar y eliminar la dirección actual
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

                // Limpiar la referencia
                user.AddressId = null;
            }
            return;
        }

        // Caso 3: Se envía dirección con datos - CREAR/ACTUALIZAR
        if (user.AddressId.HasValue)
        {
            // Usuario tiene dirección - ACTUALIZAR la existente
            var existingAddress = await _dbContext.Addresses.FirstOrDefaultAsync(
                a => a.Id == user.AddressId.Value,
                cancellationToken
            );

            if (existingAddress != null)
            {
                // Actualizar dirección existente
                UpdateAddressFields(existingAddress, addressDto);
                _logger.LogDebug(
                    "Updated existing address: {AddressId} for user: {UserId}",
                    existingAddress.Id,
                    user.Id
                );
            }
            else
            {
                // La dirección referenciada no existe - crear nueva
                await CreateNewAddressAndAssignAsync(user, addressDto, cancellationToken);
            }
        }
        else
        {
            // Usuario NO tiene dirección - CREAR nueva
            await CreateNewAddressAndAssignAsync(user, addressDto, cancellationToken);
        }
    }

    /// <summary>
    /// Actualiza los campos de una dirección existente
    /// </summary>
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

    /// <summary>
    /// Crea una nueva dirección y la asigna al usuario
    /// </summary>
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

        // ASIGNAR la nueva dirección al usuario
        user.AddressId = newAddress.Id;

        _logger.LogDebug(
            "Created new address: {AddressId} for user: {UserId}",
            newAddress.Id,
            user.Id
        );
    }

    /// <summary>
    /// Determina si una dirección está vacía (solo StateId requerido)
    /// </summary>
    private bool IsAddressEmpty(AddressDTO addressDto)
    {
        return addressDto.StateId <= 0;
    }

    /// <summary>
    /// Valida que la dirección sea válida (solo USA soportado)
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
