using Applications.DTOs.AddressDTOs;
using AuthService.Applications.Common;
using AuthService.Applications.DTOs.CompanyDTOs;
using AuthService.Domains.Addresses;
using AuthService.Domains.Users;
using AuthService.DTOs.UserDTOs;
using AuthService.Infraestructure.Services;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserHandlers;

public class CreateUserByDeveloperHandler
    : IRequestHandler<CreateUserByDeveloperCommand, ApiResponse<UserGetDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CreateUserByDeveloperHandler> _logger;
    private readonly IPasswordHash _passwordHash;

    private const int USA = 220;

    public CreateUserByDeveloperHandler(
        ApplicationDbContext dbContext,
        ILogger<CreateUserByDeveloperHandler> logger,
        IPasswordHash passwordHash
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _passwordHash = passwordHash;
    }

    public async Task<ApiResponse<UserGetDTO>> Handle(
        CreateUserByDeveloperCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dto = request.UserData;

            // 1. Verificar que la company existe
            var company = await _dbContext.Companies.FirstOrDefaultAsync(
                c => c.Id == dto.CompanyId,
                cancellationToken
            );

            if (company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", dto.CompanyId);
                return new ApiResponse<UserGetDTO>(false, "Company not found", null!);
            }

            // 2. Verificar que el email no existe
            var emailExists = await _dbContext.TaxUsers.AnyAsync(
                u => u.Email == dto.Email,
                cancellationToken
            );

            if (emailExists)
            {
                _logger.LogWarning("Email already exists: {Email}", dto.Email);
                return new ApiResponse<UserGetDTO>(false, "Email already registered", null!);
            }

            // 3. Validación de límites simplificada
            if (!dto.IgnoreUserLimit)
            {
                var currentActiveUserCount = await _dbContext.TaxUsers.CountAsync(
                    u => u.CompanyId == dto.CompanyId && u.IsActive,
                    cancellationToken
                );

                _logger.LogInformation(
                    "Creating user for company {CompanyId} (ServiceLevel: {ServiceLevel}). Current users: {CurrentUsers}",
                    dto.CompanyId,
                    company.ServiceLevel,
                    currentActiveUserCount
                );
            }

            // 4. Validar dirección si se proporciona
            Address? addressEntity = null;
            if (dto.Address is { } addressDto) // Pattern matching para null check
            {
                var validateResult = await ValidateAddressAsync(
                    addressDto.CountryId,
                    addressDto.StateId,
                    cancellationToken
                );

                if (!validateResult.Success)
                {
                    return new ApiResponse<UserGetDTO>(false, validateResult.Message, null!);
                }

                addressEntity = new Address
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

                await _dbContext.Addresses.AddAsync(addressEntity, cancellationToken);
            }

            // 5. Validar roles si se especifican
            if (dto.RoleIds?.Any() == true)
            {
                var validationResult = await ValidateRolesForServiceLevelAsync(
                    dto.RoleIds,
                    company.ServiceLevel,
                    cancellationToken
                );

                if (!validationResult.IsValid)
                {
                    return new ApiResponse<UserGetDTO>(false, validationResult.Message, null!);
                }
            }

            // 6. Crear TaxUser
            var taxUser = new TaxUser
            {
                Id = Guid.NewGuid(),
                CompanyId = dto.CompanyId,
                Email = dto.Email,
                Password = _passwordHash.HashPassword(dto.Password),
                Name = dto.Name,
                LastName = dto.LastName,
                PhoneNumber = dto.PhoneNumber,
                PhotoUrl = dto.PhotoUrl,
                AddressId = addressEntity?.Id,
                IsActive = true,
                IsOwner = dto.IsOwner,
                Confirm = true,
                OtpVerified = false,
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.TaxUsers.AddAsync(taxUser, cancellationToken);

            // 7. Asignar roles
            var rolesToAssign = await GetRolesToAssignAsync(
                dto,
                company.ServiceLevel,
                cancellationToken
            );

            foreach (var roleId in rolesToAssign.Distinct())
            {
                var userRole = new UserRole
                {
                    Id = Guid.NewGuid(),
                    TaxUserId = taxUser.Id,
                    RoleId = roleId,
                    CreatedAt = DateTime.UtcNow,
                };

                await _dbContext.UserRoles.AddAsync(userRole, cancellationToken);
            }

            // 8. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<UserGetDTO>(false, "Failed to create user", null!);
            }

            await transaction.CommitAsync(cancellationToken);

            // 9. Obtener usuario creado usando JOINs potentes
            var userDto = await GetCreatedUserWithJoinsAsync(taxUser.Id, cancellationToken);

            if (userDto == null)
            {
                _logger.LogError("Failed to retrieve created user: {UserId}", taxUser.Id);
                return new ApiResponse<UserGetDTO>(
                    false,
                    "User created but failed to retrieve",
                    null!
                );
            }

            _logger.LogInformation(
                "User created by developer: {UserId} (IsOwner: {IsOwner}) for company {CompanyId} (ServiceLevel: {ServiceLevel})",
                taxUser.Id,
                taxUser.IsOwner,
                dto.CompanyId,
                company.ServiceLevel
            );

            return new ApiResponse<UserGetDTO>(true, "User created successfully", userDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating user by developer");
            return new ApiResponse<UserGetDTO>(false, "Error creating user", null!);
        }
    }

    #region Helper Methods

    private async Task<UserGetDTO?> GetCreatedUserWithJoinsAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        // JOIN potente para obtener toda la información del usuario creado
        var userQuery =
            from u in _dbContext.TaxUsers
            join c in _dbContext.Companies on u.CompanyId equals c.Id
            join a in _dbContext.Addresses on u.AddressId equals a.Id into addresses
            from a in addresses.DefaultIfEmpty()
            join country in _dbContext.Countries on a.CountryId equals country.Id into countries
            from country in countries.DefaultIfEmpty()
            join state in _dbContext.States on a.StateId equals state.Id into states
            from state in states.DefaultIfEmpty()
            join ca in _dbContext.Addresses on c.AddressId equals ca.Id into companyAddresses
            from ca in companyAddresses.DefaultIfEmpty()
            join ccountry in _dbContext.Countries
                on ca.CountryId equals ccountry.Id
                into companyCountries
            from ccountry in companyCountries.DefaultIfEmpty()
            join cstate in _dbContext.States on ca.StateId equals cstate.Id into companyStates
            from cstate in companyStates.DefaultIfEmpty()
            where u.Id == userId
            select new UserGetDTO
            {
                Id = u.Id,
                CompanyId = u.CompanyId,
                Email = u.Email,
                IsOwner = u.IsOwner,
                Name = u.Name,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                PhotoUrl = u.PhotoUrl,
                IsActive = u.IsActive,
                Confirm = u.Confirm ?? false,
                CreatedAt = u.CreatedAt,

                // Dirección del usuario
                Address =
                    a != null
                        ? new AddressDTO
                        {
                            CountryId = a.CountryId,
                            StateId = a.StateId,
                            City = a.City,
                            Street = a.Street,
                            Line = a.Line,
                            ZipCode = a.ZipCode,
                            CountryName = country.Name,
                            StateName = state.Name,
                        }
                        : null,

                // Información de la company
                CompanyFullName = c.FullName,
                CompanyName = c.CompanyName,
                CompanyBrand = c.Brand,
                CompanyIsIndividual = !c.IsCompany,
                CompanyDomain = c.Domain,
                CompanyServiceLevel = c.ServiceLevel,

                // Dirección de la company
                CompanyAddress =
                    ca != null
                        ? new AddressDTO
                        {
                            CountryId = ca.CountryId,
                            StateId = ca.StateId,
                            City = ca.City,
                            Street = ca.Street,
                            Line = ca.Line,
                            ZipCode = ca.ZipCode,
                            CountryName = ccountry.Name,
                            StateName = cstate.Name,
                        }
                        : null,

                RoleNames = new List<string>(),
                CustomPermissions = new List<string>(),
            };

        var userDto = await userQuery.FirstOrDefaultAsync(cancellationToken);

        if (userDto != null)
        {
            // Obtener roles y permisos en consultas separadas
            await PopulateUserRolesAndPermissionsAsync(
                new List<UserGetDTO> { userDto },
                cancellationToken
            );
        }

        return userDto;
    }

    private async Task PopulateUserRolesAndPermissionsAsync(
        List<UserGetDTO> users,
        CancellationToken cancellationToken
    )
    {
        if (!users.Any())
            return;

        var userIds = users.Select(u => u.Id).ToList();

        // Obtener roles
        var rolesQuery =
            from ur in _dbContext.UserRoles
            join r in _dbContext.Roles on ur.RoleId equals r.Id
            where userIds.Contains(ur.TaxUserId)
            select new { ur.TaxUserId, r.Name };

        var userRoles = await rolesQuery.ToListAsync(cancellationToken);
        var rolesByUser = userRoles
            .GroupBy(x => x.TaxUserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Name).ToList());

        // Obtener permisos personalizados
        var permissionsQuery =
            from cp in _dbContext.CompanyPermissions
            join p in _dbContext.Permissions on cp.PermissionId equals p.Id
            where userIds.Contains(cp.TaxUserId) && cp.IsGranted
            select new { cp.TaxUserId, p.Code };

        var userPermissions = await permissionsQuery.ToListAsync(cancellationToken);
        var permissionsByUser = userPermissions
            .GroupBy(x => x.TaxUserId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Code).ToList());

        // Asignar a cada usuario
        foreach (var user in users)
        {
            if (rolesByUser.TryGetValue(user.Id, out var roles))
            {
                user.RoleNames = roles;
            }

            if (permissionsByUser.TryGetValue(user.Id, out var permissions))
            {
                user.CustomPermissions = permissions;
            }
        }
    }

    private async Task<List<Guid>> GetRolesToAssignAsync(
        CreateUserByDeveloperDTO dto,
        ServiceLevel companyServiceLevel,
        CancellationToken cancellationToken
    )
    {
        var rolesToAssign = new List<Guid>();

        if (dto.RoleIds?.Any() == true)
        {
            rolesToAssign.AddRange(dto.RoleIds);
        }
        else
        {
            // Rol por defecto según IsOwner y ServiceLevel
            var defaultRoleName = GetDefaultRoleName(dto.IsOwner, companyServiceLevel);
            var defaultRoleId = await _dbContext
                .Roles.Where(r => r.Name == defaultRoleName)
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (defaultRoleId != Guid.Empty)
            {
                rolesToAssign.Add(defaultRoleId);
            }
        }

        return rolesToAssign;
    }

    private async Task<(bool IsValid, string Message)> ValidateRolesForServiceLevelAsync(
        ICollection<Guid> roleIds,
        ServiceLevel companyServiceLevel,
        CancellationToken cancellationToken
    )
    {
        var rolesQuery =
            from r in _dbContext.Roles
            where roleIds.Contains(r.Id)
            select new
            {
                r.Id,
                r.Name,
                r.ServiceLevel,
            };

        var roles = await rolesQuery.ToListAsync(cancellationToken);

        foreach (var role in roles)
        {
            if (role.ServiceLevel.HasValue && role.ServiceLevel > companyServiceLevel)
            {
                return (
                    false,
                    $"Role '{role.Name}' requires {role.ServiceLevel} service level, but company has {companyServiceLevel}"
                );
            }
        }

        return (true, "Roles validated successfully");
    }

    private static string GetDefaultRoleName(bool isOwner, ServiceLevel serviceLevel)
    {
        if (isOwner)
        {
            return serviceLevel switch
            {
                ServiceLevel.Basic => "Administrator Basic",
                ServiceLevel.Standard => "Administrator Standard",
                ServiceLevel.Pro => "Administrator Pro",
                ServiceLevel.Developer => "Developer",
                _ => "Administrator Basic",
            };
        }

        return "User";
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
            select new { CountryName = c.Name, StateName = s.Name }
        ).FirstOrDefaultAsync(cancellationToken);

        if (addressValidation == null)
        {
            return (false, $"Invalid CountryId '{countryId}' or StateId '{stateId}'");
        }

        return (true, "Address validation passed");
    }

    #endregion
}
