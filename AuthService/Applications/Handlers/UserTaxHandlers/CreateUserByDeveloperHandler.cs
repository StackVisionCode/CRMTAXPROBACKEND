using AuthService.Domains.Addresses;
using AuthService.Domains.Users;
using AuthService.DTOs.UserDTOs;
using AuthService.Infraestructure.Services;
using AutoMapper;
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
    private readonly IMapper _mapper;

    private const int USA = 220;

    public CreateUserByDeveloperHandler(
        ApplicationDbContext dbContext,
        ILogger<CreateUserByDeveloperHandler> logger,
        IPasswordHash passwordHash,
        IMapper mapper
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _passwordHash = passwordHash;
        _mapper = mapper;
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

            // 1. Verificar que la company existe y obtener info del plan
            var companyQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == dto.CompanyId
                select new
                {
                    Company = c,
                    CustomPlan = cp,
                    UserLimit = cp.UserLimit, // 🆕 USAR CustomPlan.UserLimit directamente
                    CurrentActiveUserCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == dto.CompanyId && u.IsActive
                    ),
                };

            var companyData = await companyQuery.FirstOrDefaultAsync(cancellationToken);
            if (companyData?.Company == null)
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

            // 3. Verificar límites del plan (developers pueden omitir esto si es necesario)
            if (
                !dto.IgnoreUserLimit
                && // flag para developers que pueden omitir límites
                companyData.CurrentActiveUserCount >= companyData.UserLimit
            )
            {
                _logger.LogWarning(
                    "User limit exceeded for company: {CompanyId}. Current: {Current}, Limit: {Limit}",
                    dto.CompanyId,
                    companyData.CurrentActiveUserCount,
                    companyData.UserLimit
                );
                return new ApiResponse<UserGetDTO>(
                    false,
                    $"User limit exceeded. Plan allows {companyData.UserLimit} users, currently has {companyData.CurrentActiveUserCount}",
                    null!
                );
            }

            // 4. Crear dirección si se proporciona
            Address? addressEntity = null;
            if (dto.Address != null)
            {
                var validateResult = await ValidateAddressAsync(
                    dto.Address.CountryId,
                    dto.Address.StateId,
                    cancellationToken
                );
                if (!validateResult.Success)
                {
                    return new ApiResponse<UserGetDTO>(false, validateResult.Message, null!);
                }

                addressEntity = new Address
                {
                    Id = Guid.NewGuid(),
                    CountryId = dto.Address.CountryId,
                    StateId = dto.Address.StateId,
                    City = dto.Address.City?.Trim(),
                    Street = dto.Address.Street?.Trim(),
                    Line = dto.Address.Line?.Trim(),
                    ZipCode = dto.Address.ZipCode?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                };

                await _dbContext.Addresses.AddAsync(addressEntity, cancellationToken);
            }

            // 5. Crear TaxUser - ACTIVO y CONFIRMADO para developers
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

            // 6. Asignar roles
            var rolesToAssign = new List<Guid>();
            if (dto.RoleIds?.Any() == true)
            {
                rolesToAssign.AddRange(dto.RoleIds);
            }
            else
            {
                // Rol por defecto según IsOwner
                var defaultRoleName = dto.IsOwner ? "Administrator Basic" : "User";
                var defaultRoleQuery =
                    from r in _dbContext.Roles
                    where r.Name == defaultRoleName
                    select r.Id;
                var defaultRoleId = await defaultRoleQuery.FirstOrDefaultAsync(cancellationToken);
                if (defaultRoleId != Guid.Empty)
                {
                    rolesToAssign.Add(defaultRoleId);
                }
            }

            // Crear UserRoles
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

            // 7. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<UserGetDTO>(false, "Failed to create user", null!);
            }

            await transaction.CommitAsync(cancellationToken);

            // 8. Obtener TaxUser completo para respuesta
            var createdUserQuery =
                from u in _dbContext.TaxUsers
                join c in _dbContext.Companies on u.CompanyId equals c.Id
                where u.Id == taxUser.Id
                select u;

            var createdUser = await createdUserQuery
                .Include(u => u.Address)
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(cancellationToken);

            var userDto = _mapper.Map<UserGetDTO>(createdUser);

            _logger.LogInformation(
                "User created by developer: {UserId} (IsOwner: {IsOwner}). Users: {Current}/{Limit}",
                taxUser.Id,
                taxUser.IsOwner,
                companyData.CurrentActiveUserCount + 1,
                companyData.UserLimit
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

    private async Task<(bool Success, string Message)> ValidateAddressAsync(
        int countryId,
        int stateId,
        CancellationToken ct
    )
    {
        if (countryId != USA)
            return (false, "Only United States (CountryId = 220) is supported.");

        var country = await _dbContext
            .Countries.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == countryId, ct);
        if (country is null)
            return (false, $"CountryId '{countryId}' not found.");

        var state = await _dbContext
            .States.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == stateId && s.CountryId == countryId, ct);
        if (state is null)
            return (false, $"StateId '{stateId}' not found for CountryId '{countryId}'.");

        return (true, "OK");
    }
}
