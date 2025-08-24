using AuthService.Commands.InvitationCommands;
using AuthService.Domains.Addresses;
using AuthService.Domains.Permissions;
using AuthService.Domains.Users;
using AuthService.Infraestructure.Services;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.InvitationEvents;

namespace Handlers.UserHandlers;

public class RegisterUserByInvitationHandler
    : IRequestHandler<RegisterUserByInvitationCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RegisterUserByInvitationHandler> _logger;
    private readonly IInvitationTokenService _invitationTokenService;
    private readonly IPasswordHash _passwordHash;
    private readonly IEventBus _eventBus;

    private const int USA = 220;

    public RegisterUserByInvitationHandler(
        ApplicationDbContext dbContext,
        ILogger<RegisterUserByInvitationHandler> logger,
        IInvitationTokenService invitationTokenService,
        IPasswordHash passwordHash,
        IEventBus eventBus
    )
    {
        _dbContext = dbContext;
        _logger = logger;
        _invitationTokenService = invitationTokenService;
        _passwordHash = passwordHash;
        _eventBus = eventBus;
    }

    public async Task<ApiResponse<bool>> Handle(
        RegisterUserByInvitationCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var registration = request.Registration;

            // 1. Validar token de invitación
            var (isValid, companyId, email, roleIds, errorMessage) =
                _invitationTokenService.ValidateInvitation(registration.InvitationToken);
            if (!isValid)
            {
                _logger.LogWarning("Invalid invitation token: {Error}", errorMessage);
                return new ApiResponse<bool>(
                    false,
                    errorMessage ?? "Invalid invitation token",
                    false
                );
            }

            // 2. Verificar que la company aún existe y obtener límites + información del Administrator
            var companyQuery =
                from c in _dbContext.Companies
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where c.Id == companyId
                select new
                {
                    c.Id,
                    c.CompanyName,
                    c.FullName,
                    c.Domain,
                    c.IsCompany,
                    CustomPlanId = cp.Id,
                    CustomPlanIsActive = cp.IsActive,
                    UserLimit = cp.UserLimit,
                    CurrentActiveUserCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == companyId && u.IsActive
                    ),
                    OwnerCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == companyId && u.IsOwner && u.IsActive
                    ),
                };

            var company = await companyQuery.FirstOrDefaultAsync(cancellationToken);
            if (company == null)
            {
                _logger.LogWarning("Company not found during registration: {CompanyId}", companyId);
                return new ApiResponse<bool>(false, "Company no longer exists", false);
            }

            if (!company.CustomPlanIsActive)
            {
                _logger.LogWarning("Company plan is inactive: {CompanyId}", companyId);
                return new ApiResponse<bool>(false, "Company plan is inactive", false);
            }

            // 3. Verificar que el email no esté registrado
            var emailExists = await _dbContext.TaxUsers.AnyAsync(
                u => u.Email == email,
                cancellationToken
            );

            if (emailExists)
            {
                _logger.LogWarning("Email already registered: {Email}", email);
                return new ApiResponse<bool>(false, "Email already registered", false);
            }

            if (company.CurrentActiveUserCount >= company.UserLimit)
            {
                _logger.LogWarning(
                    "User limit exceeded during registration for company: {CompanyId}. "
                        + "Current active users: {Current}, CustomPlan limit: {Limit}",
                    companyId,
                    company.CurrentActiveUserCount,
                    company.UserLimit
                );
                return new ApiResponse<bool>(
                    false,
                    $"User limit exceeded. Current: {company.CurrentActiveUserCount}, Limit: {company.UserLimit}",
                    false
                );
            }

            if (company.OwnerCount == 0)
            {
                _logger.LogError("Company {CompanyId} has no active Owner", companyId);
                return new ApiResponse<bool>(false, "Company has no active administrator", false);
            }

            // 4. Obtener todos los permisos del Administrator de la company (Owner)
            var adminPermissionsQuery =
                from adminUser in _dbContext.TaxUsers
                join userRole in _dbContext.UserRoles on adminUser.Id equals userRole.TaxUserId
                join role in _dbContext.Roles on userRole.RoleId equals role.Id
                join rolePermission in _dbContext.RolePermissions
                    on role.Id equals rolePermission.RoleId
                join permission in _dbContext.Permissions
                    on rolePermission.PermissionId equals permission.Id
                where
                    adminUser.CompanyId == companyId
                    && adminUser.IsOwner == true
                    && adminUser.IsActive == true
                    && role.Name.Contains("Administrator") // Administrator Basic, Standard, Pro
                    && permission.IsGranted == true
                select new
                {
                    PermissionId = permission.Id,
                    PermissionCode = permission.Code,
                    PermissionName = permission.Name,
                };

            var adminPermissions = await adminPermissionsQuery
                .Distinct()
                .ToListAsync(cancellationToken);

            if (!adminPermissions.Any())
            {
                _logger.LogWarning(
                    "No administrator permissions found for company {CompanyId}. "
                        + "This might indicate a data integrity issue.",
                    companyId
                );
            }

            _logger.LogDebug(
                "Found {PermissionCount} administrator permissions to inherit for company {CompanyId}",
                adminPermissions.Count,
                companyId
            );

            // 5. Crear dirección si se proporciona
            Address? userAddressEntity = null;
            if (registration.Address != null)
            {
                var addrDto = registration.Address;
                var validateResult = await ValidateAddressAsync(
                    addrDto.CountryId,
                    addrDto.StateId,
                    cancellationToken
                );
                if (!validateResult.Success)
                {
                    return new ApiResponse<bool>(false, validateResult.Message, false);
                }

                userAddressEntity = new Address
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

                await _dbContext.Addresses.AddAsync(userAddressEntity, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);

                _logger.LogDebug("Created address for TaxUser: {AddressId}", userAddressEntity.Id);
            }

            // 6. Crear TaxUser - ACTIVO, CONFIRMADO y NO OWNER
            var taxUser = new TaxUser
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                Email = email,
                Password = _passwordHash.HashPassword(registration.Password),
                Name = registration.Name,
                LastName = registration.LastName,
                PhoneNumber = registration.PhoneNumber,
                PhotoUrl = registration.PhotoUrl,
                AddressId = userAddressEntity?.Id,
                IsActive = true,
                IsOwner = false,
                Confirm = true,
                OtpVerified = false,
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.TaxUsers.AddAsync(taxUser, cancellationToken);

            // 7. Asignar roles
            var rolesToAssign = new List<Guid>();

            // Rol User por defecto si no se especificaron roles
            if (roleIds?.Any() != true)
            {
                var userRoleQuery = from r in _dbContext.Roles where r.Name == "User" select r.Id;
                var userRoleId = await userRoleQuery.FirstOrDefaultAsync(cancellationToken);
                if (userRoleId != Guid.Empty)
                {
                    rolesToAssign.Add(userRoleId);
                }
            }
            else
            {
                // Validar que los roles especificados existen y no incluyen roles de Administrator
                var validRoles = await _dbContext
                    .Roles.Where(r =>
                        roleIds.Contains(r.Id)
                        && !r.Name.Contains("Administrator")
                        && !r.Name.Contains("Developer")
                    )
                    .Select(r => r.Id)
                    .ToListAsync(cancellationToken);

                rolesToAssign.AddRange(validRoles);

                // Si no hay roles válidos, asignar User por defecto
                if (!rolesToAssign.Any())
                {
                    var userRoleQuery =
                        from r in _dbContext.Roles
                        where r.Name == "User"
                        select r.Id;
                    var userRoleId = await userRoleQuery.FirstOrDefaultAsync(cancellationToken);
                    if (userRoleId != Guid.Empty)
                    {
                        rolesToAssign.Add(userRoleId);
                    }
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

            // 8. HEREDAR PERMISOS DEL ADMINISTRATOR A COMPANYPERMISSIONS
            var companyPermissionsToAdd = new List<CompanyPermission>();

            foreach (var adminPermission in adminPermissions)
            {
                var companyPermission = new CompanyPermission
                {
                    Id = Guid.NewGuid(),
                    TaxUserId = taxUser.Id,
                    PermissionId = adminPermission.PermissionId,
                    IsGranted = true, // Heredamos como granted
                    Description =
                        $"Inherited from Administrator role on registration - {adminPermission.PermissionCode}",
                    CreatedAt = DateTime.UtcNow,
                };

                companyPermissionsToAdd.Add(companyPermission);
            }

            // Bulk insert para mejor performance
            if (companyPermissionsToAdd.Any())
            {
                await _dbContext.CompanyPermissions.AddRangeAsync(
                    companyPermissionsToAdd,
                    cancellationToken
                );

                _logger.LogInformation(
                    "Inherited {PermissionCount} administrator permissions for new user {TaxUserId} "
                        + "in company {CompanyId}",
                    companyPermissionsToAdd.Count,
                    taxUser.Id,
                    companyId
                );
            }

            // 9. Guardar todo
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to create TaxUser by invitation");
                return new ApiResponse<bool>(false, "Failed to create user account", false);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "TaxUser registered successfully by invitation: {TaxUserId} for company {CompanyId}. "
                    + "Users: {Current}/{Limit}. Inherited permissions: {PermissionCount}",
                taxUser.Id,
                companyId,
                company.CurrentActiveUserCount + 1, // +1 porque acabamos de crear uno
                company.UserLimit,
                companyPermissionsToAdd.Count
            );

            // 10. Publicar evento de registro completado
            _eventBus.Publish(
                new UserRegisteredEvent(
                    Id: Guid.NewGuid(),
                    OccurredOn: DateTime.UtcNow,
                    TaxUserId: taxUser.Id,
                    Email: taxUser.Email,
                    Name: taxUser.Name,
                    LastName: taxUser.LastName,
                    CompanyId: companyId,
                    CompanyName: company.CompanyName,
                    CompanyFullName: company.FullName,
                    CompanyDomain: company.Domain,
                    IsCompany: company.IsCompany
                )
            );

            return new ApiResponse<bool>(true, "Registration completed successfully", true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error during TaxUser registration by invitation: {Message}",
                ex.Message
            );
            return new ApiResponse<bool>(false, ex.Message, false);
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
