using AuthService.Commands.InvitationCommands;
using AuthService.Domains.Addresses;
using AuthService.Domains.Permissions;
using AuthService.Domains.Roles;
using AuthService.Domains.UserCompanies;
using AuthService.Infraestructure.Services;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.InvitationEvents;

namespace AuthService.Handlers.InvitationHandlers;

public class RegisterUserCompanyByInvitationHandler
    : IRequestHandler<RegisterUserCompanyByInvitationCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RegisterUserCompanyByInvitationHandler> _logger;
    private readonly IInvitationTokenService _invitationTokenService;
    private readonly IPasswordHash _passwordHash;
    private readonly IEventBus _eventBus;

    private const int USA = 220;

    public RegisterUserCompanyByInvitationHandler(
        ApplicationDbContext dbContext,
        ILogger<RegisterUserCompanyByInvitationHandler> logger,
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
        RegisterUserCompanyByInvitationCommand request,
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

            // 2. Verificar que la company aún existe
            var companyQuery =
                from c in _dbContext.Companies
                where c.Id == companyId
                select new
                {
                    c.Id,
                    c.CompanyName,
                    c.FullName,
                    c.Domain,
                    c.IsCompany,
                    c.CustomPlan,
                    CurrentUserCompanyCount = c.UserCompanies.Count(),
                };

            var company = await companyQuery.FirstOrDefaultAsync(cancellationToken);
            if (company == null)
            {
                _logger.LogWarning("Company not found during registration: {CompanyId}", companyId);
                return new ApiResponse<bool>(false, "Company no longer exists", false);
            }

            // 3. Verificar que el email no se haya registrado mientras tanto
            var emailExists =
                await _dbContext.UserCompanies.AnyAsync(uc => uc.Email == email, cancellationToken)
                || await _dbContext.TaxUsers.AnyAsync(u => u.Email == email, cancellationToken);

            if (emailExists)
            {
                _logger.LogWarning("Email already registered: {Email}", email);
                return new ApiResponse<bool>(false, "Email already registered", false);
            }

            // 4. Verificar límite de usuarios nuevamente
            var serviceQuery =
                from cm in _dbContext.CustomModules
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                join s in _dbContext.Services on m.ServiceId equals s.Id
                where cm.CustomPlanId == company.CustomPlan.Id && cm.IsIncluded
                select s.UserLimit;

            var userLimit = await serviceQuery.FirstOrDefaultAsync(cancellationToken);
            if (userLimit > 0 && company.CurrentUserCompanyCount >= userLimit)
            {
                _logger.LogWarning(
                    "User limit exceeded during registration for company: {CompanyId}",
                    companyId
                );
                return new ApiResponse<bool>(false, "User limit exceeded", false);
            }

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

                _logger.LogDebug(
                    "Created address for UserCompany: {AddressId}",
                    userAddressEntity.Id
                );
            }

            // 6. Crear UserCompany - ACTIVO Y CONFIRMADO desde el inicio
            var userCompany = new UserCompany
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
                IsActive = true, // ✅ ACTIVO desde el inicio
                Confirm = true, // ✅ CONFIRMADO desde el inicio
                OtpVerified = false,
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.UserCompanies.AddAsync(userCompany, cancellationToken);

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
                rolesToAssign.AddRange(roleIds);
            }

            // Crear UserCompanyRoles
            var createdUserCompanyRoles = new List<UserCompanyRole>();
            foreach (var roleId in rolesToAssign.Distinct())
            {
                var userCompanyRole = new UserCompanyRole
                {
                    Id = Guid.NewGuid(),
                    UserCompanyId = userCompany.Id,
                    RoleId = roleId,
                    CreatedAt = DateTime.UtcNow,
                };

                await _dbContext.UserCompanyRoles.AddAsync(userCompanyRole, cancellationToken);
                createdUserCompanyRoles.Add(userCompanyRole);
            }

            // Asignar permisos del plan de la company
            await AssignCompanyPlanPermissionsAsync(
                userCompany.Id,
                createdUserCompanyRoles,
                company.CustomPlan.Id,
                cancellationToken
            );

            // 8. Guardar todo
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to create UserCompany");
                return new ApiResponse<bool>(false, "Failed to create user account", false);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "UserCompany registered successfully by invitation: {UserCompanyId} for company {CompanyId}",
                userCompany.Id,
                companyId
            );

            // 9. Publicar evento de registro completado (NO de confirmación, ya está activo)
            _eventBus.Publish(
                new UserCompanyRegisteredEvent(
                    Id: Guid.NewGuid(),
                    OccurredOn: DateTime.UtcNow,
                    UserCompanyId: userCompany.Id,
                    Email: userCompany.Email,
                    Name: userCompany.Name,
                    LastName: userCompany.LastName,
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
                "Error during UserCompany registration by invitation: {Message}",
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

    /// <summary> Helper
    /// Copia los permisos del plan de la company al nuevo UserCompany
    /// Basado en los permisos que ya tiene la company según su CustomPlan y Service
    /// </summary>
    private async Task AssignCompanyPlanPermissionsAsync(
        Guid userCompanyId,
        List<UserCompanyRole> userCompanyRoles,
        Guid customPlanId,
        CancellationToken ct
    )
    {
        try
        {
            // Obtener los permisos disponibles según el CustomPlan de la company
            var planPermissionsQuery =
                from cp in _dbContext.CustomPlans
                join cm in _dbContext.CustomModules on cp.Id equals cm.CustomPlanId
                join m in _dbContext.Modules on cm.ModuleId equals m.Id
                join s in _dbContext.Services on m.ServiceId equals s.Id
                join sm in _dbContext.Modules on s.Id equals sm.ServiceId // Módulos del service
                join srp in _dbContext.RolePermissions on true equals true // Cross join para permisos del service
                join sr in _dbContext.Roles on srp.RoleId equals sr.Id
                join sp in _dbContext.Permissions on srp.PermissionId equals sp.Id
                where
                    cp.Id == customPlanId
                    && cm.IsIncluded
                    && m.IsActive
                    && s.IsActive
                    && (sr.Name.Contains("Administrator") || sr.Name == "User") // Solo roles de company
                select new
                {
                    PermissionId = sp.Id,
                    PermissionName = sp.Name,
                    PermissionCode = sp.Code,
                    ModuleName = m.Name,
                };

            var availablePermissions = await planPermissionsQuery.Distinct().ToListAsync(ct);

            if (!availablePermissions.Any())
            {
                _logger.LogWarning(
                    "No permissions found for CustomPlan {CustomPlanId}",
                    customPlanId
                );
                return;
            }

            // Crear CompanyPermissions para cada permiso disponible y cada rol del usuario
            var companyPermissions = new List<CompanyPermission>();

            foreach (var userCompanyRole in userCompanyRoles)
            {
                foreach (var permission in availablePermissions)
                {
                    var companyPermission = new CompanyPermission
                    {
                        Id = Guid.NewGuid(),
                        UserCompanyId = userCompanyId,
                        UserCompanyRoleId = userCompanyRole.Id,
                        Name = permission.PermissionName,
                        Code = permission.PermissionCode,
                        Description = $"Permission from {permission.ModuleName} module",
                        IsGranted = true,
                        CreatedAt = DateTime.UtcNow,
                    };

                    companyPermissions.Add(companyPermission);
                }
            }

            // Eliminar duplicados por Code
            var uniquePermissions = companyPermissions
                .GroupBy(cp => new { cp.UserCompanyRoleId, cp.Code })
                .Select(g => g.First())
                .ToList();

            // Guardar permisos únicos
            if (uniquePermissions.Any())
            {
                await _dbContext.CompanyPermissions.AddRangeAsync(uniquePermissions, ct);

                _logger.LogInformation(
                    "Assigned {PermissionCount} company permissions to UserCompany {UserCompanyId} based on CustomPlan {CustomPlanId}",
                    uniquePermissions.Count,
                    userCompanyId,
                    customPlanId
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error copying company plan permissions to UserCompany {UserCompanyId}",
                userCompanyId
            );
        }
    }
}
