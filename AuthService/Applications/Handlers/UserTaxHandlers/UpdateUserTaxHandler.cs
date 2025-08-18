using Applications.DTOs.CompanyDTOs;
using AuthService.Domains.Addresses;
using AuthService.Domains.Users;
using AuthService.Infraestructure.Services;
using AutoMapper;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserTaxHandlers;

public class UpdateUserTaxHandler : IRequestHandler<UpdateTaxUserCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateUserTaxHandler> _logger;
    private readonly IPasswordHash _passwordHash;

    private const int USA = 220;

    public UpdateUserTaxHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<UpdateUserTaxHandler> logger,
        IPasswordHash passwordHash
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
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

            // Guardar valores actuales
            var currentAddressId = user.AddressId;
            var currentPassword = user.Password;
            var currentIsOwner = user.IsOwner;
            var currentCompanyId = user.CompanyId;

            // Actualizar campos del usuario
            _mapper.Map(request.UserTax, user);

            // Restaurar campos que NO se pueden cambiar via DTO
            user.AddressId = currentAddressId;
            user.IsOwner = currentIsOwner; // No se puede cambiar via UpdateUserDTO
            user.CompanyId = currentCompanyId; // No se puede cambiar via UpdateUserDTO

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
            if (request.UserTax.Address is not null)
            {
                var addrDto = request.UserTax.Address;
                var validateResult = await ValidateAddressAsync(
                    addrDto.CountryId,
                    addrDto.StateId,
                    cancellationToken
                );
                if (!validateResult.Success)
                    return new ApiResponse<bool>(false, validateResult.Message, false);

                if (user.AddressId.HasValue)
                {
                    var addressQuery =
                        from a in _dbContext.Addresses
                        where a.Id == user.AddressId.Value
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
                            "Updated existing address: {AddressId} for tax user: {UserId}",
                            existingAddress.Id,
                            user.Id
                        );
                    }
                    else
                    {
                        await CreateNewAddressAsync(user, addrDto, cancellationToken);
                    }
                }
                else
                {
                    await CreateNewAddressAsync(user, addrDto, cancellationToken);
                }
            }
            else if (user.AddressId.HasValue)
            {
                var addressToDeleteQuery =
                    from a in _dbContext.Addresses
                    where a.Id == user.AddressId.Value
                    select a;

                var addressToDelete = await addressToDeleteQuery.FirstOrDefaultAsync(
                    cancellationToken
                );
                if (addressToDelete != null)
                {
                    _dbContext.Addresses.Remove(addressToDelete);
                    _logger.LogDebug(
                        "Removed address: {AddressId} for tax user: {UserId}",
                        addressToDelete.Id,
                        user.Id
                    );
                }
                user.AddressId = null;
            }

            // Actualizar roles con validaciones especiales para Owner
            if (request.UserTax.RoleIds?.Any() == true)
            {
                // Validación especial para Owner
                if (user.IsOwner)
                {
                    // Verificar que el Owner mantenga al menos un rol Administrator
                    var newRoleNamesQuery =
                        from r in _dbContext.Roles
                        where request.UserTax.RoleIds.Contains(r.Id)
                        select r.Name;

                    var newRoleNames = await newRoleNamesQuery.ToListAsync(cancellationToken);
                    var hasAdminRole = newRoleNames.Any(name =>
                        name.Contains("Administrator") || name == "Developer"
                    );

                    if (!hasAdminRole)
                    {
                        _logger.LogWarning(
                            "Owner must have at least one Administrator role: {UserId}",
                            user.Id
                        );
                        return new ApiResponse<bool>(
                            false,
                            "Company owner must maintain at least one Administrator role",
                            false
                        );
                    }
                }

                // Actualizar roles
                var existingRolesQuery =
                    from ur in _dbContext.UserRoles
                    where ur.TaxUserId == user.Id
                    select ur;

                var existingRoles = await existingRolesQuery.ToListAsync(cancellationToken);
                _dbContext.UserRoles.RemoveRange(existingRoles);

                foreach (var roleId in request.UserTax.RoleIds.Distinct())
                {
                    await _dbContext.UserRoles.AddAsync(
                        new UserRole
                        {
                            Id = Guid.NewGuid(),
                            TaxUserId = user.Id,
                            RoleId = roleId,
                            CreatedAt = DateTime.UtcNow,
                        },
                        cancellationToken
                    );
                }

                _logger.LogDebug(
                    "Updated roles for tax user: {UserId}, RoleCount: {RoleCount}",
                    user.Id,
                    request.UserTax.RoleIds.Count()
                );
            }

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
    /// Crea una nueva dirección y la asigna al usuario
    /// </summary>
    private async Task CreateNewAddressAsync(
        TaxUser user,
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
        user.AddressId = newAddress.Id;

        _logger.LogDebug(
            "Created new address: {AddressId} for tax user: {UserId}",
            newAddress.Id,
            user.Id
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

        return (true, "Address validation passed");
    }
}
