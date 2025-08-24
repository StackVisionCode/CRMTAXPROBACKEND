using AuthService.Commands.InvitationCommands;
using AuthService.Domains.Addresses;
using AuthService.Domains.Invitations;
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

            // 1. Validar token de invitación y obtener registro de invitación
            var invitationQuery =
                from i in _dbContext.Invitations
                join c in _dbContext.Companies on i.CompanyId equals c.Id
                join cp in _dbContext.CustomPlans on c.CustomPlanId equals cp.Id
                where i.Token == registration.InvitationToken
                select new
                {
                    InvitationId = i.Id,
                    i.CompanyId,
                    i.Email,
                    i.Status,
                    i.ExpiresAt,
                    i.RoleIds,
                    CompanyName = c.CompanyName,
                    CompanyFullName = c.FullName,
                    CompanyDomain = c.Domain,
                    CompanyIsCompany = c.IsCompany,
                    CustomPlanIsActive = cp.IsActive,
                    UserLimit = cp.UserLimit,
                    CurrentActiveUserCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == i.CompanyId && u.IsActive
                    ),
                    OwnerCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == i.CompanyId && u.IsOwner && u.IsActive
                    ),
                };

            var invitationData = await invitationQuery.FirstOrDefaultAsync(cancellationToken);
            if (invitationData == null)
            {
                _logger.LogWarning("Invalid invitation token during registration");
                return new ApiResponse<bool>(false, "Invalid invitation token", false);
            }

            // 2. Verificar estado de la invitación
            if (invitationData.Status != InvitationStatus.Pending)
            {
                _logger.LogWarning(
                    "Invitation is not pending: {InvitationId}, Status: {Status}",
                    invitationData.InvitationId,
                    invitationData.Status
                );
                return new ApiResponse<bool>(
                    false,
                    $"Invitation is no longer valid. Status: {invitationData.Status}",
                    false
                );
            }

            if (invitationData.ExpiresAt <= DateTime.UtcNow)
            {
                // Marcar como expirada
                await _dbContext.Database.ExecuteSqlInterpolatedAsync(
                    $@"
                    UPDATE Invitations 
                    SET Status = {(int)InvitationStatus.Expired}, UpdatedAt = {DateTime.UtcNow}
                    WHERE Id = {invitationData.InvitationId}
                ",
                    cancellationToken
                );

                _logger.LogWarning(
                    "Invitation has expired: {InvitationId}",
                    invitationData.InvitationId
                );
                return new ApiResponse<bool>(false, "Invitation has expired", false);
            }

            // 3. Verificar estado de la company
            if (!invitationData.CustomPlanIsActive)
            {
                _logger.LogWarning(
                    "Company plan is inactive during registration: {CompanyId}",
                    invitationData.CompanyId
                );
                return new ApiResponse<bool>(false, "Company plan is inactive", false);
            }

            // 4. Verificar límites de usuarios
            if (invitationData.CurrentActiveUserCount >= invitationData.UserLimit)
            {
                _logger.LogWarning(
                    "User limit exceeded during registration for company: {CompanyId}. "
                        + "Current active users: {Current}, CustomPlan limit: {Limit}",
                    invitationData.CompanyId,
                    invitationData.CurrentActiveUserCount,
                    invitationData.UserLimit
                );
                return new ApiResponse<bool>(
                    false,
                    $"User limit exceeded. Current: {invitationData.CurrentActiveUserCount}, Limit: {invitationData.UserLimit}",
                    false
                );
            }

            if (invitationData.OwnerCount == 0)
            {
                _logger.LogError(
                    "Company {CompanyId} has no active Owner",
                    invitationData.CompanyId
                );
                return new ApiResponse<bool>(false, "Company has no active administrator", false);
            }

            // 5. Verificar que el email no esté registrado
            var emailExists = await _dbContext.TaxUsers.AnyAsync(
                u => u.Email == invitationData.Email,
                cancellationToken
            );

            if (emailExists)
            {
                _logger.LogWarning("Email already registered: {Email}", invitationData.Email);
                return new ApiResponse<bool>(false, "Email already registered", false);
            }

            // 6. Obtener permisos del Administrator para heredar
            var adminPermissionsQuery =
                from adminUser in _dbContext.TaxUsers
                join userRole in _dbContext.UserRoles on adminUser.Id equals userRole.TaxUserId
                join role in _dbContext.Roles on userRole.RoleId equals role.Id
                join rolePermission in _dbContext.RolePermissions
                    on role.Id equals rolePermission.RoleId
                join permission in _dbContext.Permissions
                    on rolePermission.PermissionId equals permission.Id
                where
                    adminUser.CompanyId == invitationData.CompanyId
                    && adminUser.IsOwner == true
                    && adminUser.IsActive == true
                    && role.Name.Contains("Administrator")
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

            _logger.LogDebug(
                "Found {PermissionCount} administrator permissions to inherit for company {CompanyId}",
                adminPermissions.Count,
                invitationData.CompanyId
            );

            // 7. Crear dirección si se proporciona
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

            // 8. Crear TaxUser - ACTIVO, CONFIRMADO y NO OWNER
            var taxUser = new TaxUser
            {
                Id = Guid.NewGuid(),
                CompanyId = invitationData.CompanyId,
                Email = invitationData.Email,
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

            // 9. Asignar roles
            var rolesToAssign = new List<Guid>();

            // Si la invitación especifica roles, usarlos; sino, asignar User por defecto
            if (invitationData.RoleIds?.Any() == true)
            {
                // Validar que los roles especificados existen y no incluyen roles de Administrator
                var validRoles = await _dbContext
                    .Roles.Where(r =>
                        invitationData.RoleIds.Contains(r.Id)
                        && !r.Name.Contains("Administrator")
                        && !r.Name.Contains("Developer")
                    )
                    .Select(r => r.Id)
                    .ToListAsync(cancellationToken);

                rolesToAssign.AddRange(validRoles);
            }

            // Si no hay roles válidos, asignar User por defecto
            if (!rolesToAssign.Any())
            {
                var userRoleQuery = from r in _dbContext.Roles where r.Name == "User" select r.Id;
                var userRoleId = await userRoleQuery.FirstOrDefaultAsync(cancellationToken);
                if (userRoleId != Guid.Empty)
                {
                    rolesToAssign.Add(userRoleId);
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

            // 10. HEREDAR PERMISOS DEL ADMINISTRATOR A COMPANYPERMISSIONS
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
                    invitationData.CompanyId
                );
            }

            // 🔧 PRIMERA PARTE: Guardar TaxUser y dependencias SIN actualizar invitación
            var firstSaveResult = await _dbContext.SaveChangesAsync(cancellationToken);
            if (firstSaveResult <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to create TaxUser by invitation - first save failed");
                return new ApiResponse<bool>(false, "Failed to create user account", false);
            }

            _logger.LogInformation("TaxUser created successfully with ID: {TaxUserId}", taxUser.Id);

            //  Actualizar invitación DESPUÉS de que TaxUser existe
            var invitation = await _dbContext.Invitations.FirstOrDefaultAsync(
                i => i.Id == invitationData.InvitationId,
                cancellationToken
            );

            if (invitation == null)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(
                    "Invitation not found for update: {InvitationId}",
                    invitationData.InvitationId
                );
                return new ApiResponse<bool>(false, "Invitation not found", false);
            }

            // Actualizar invitación
            invitation.Status = InvitationStatus.Accepted;
            invitation.AcceptedAt = DateTime.UtcNow;
            invitation.RegisteredUserId = taxUser.Id;
            invitation.UpdatedAt = DateTime.UtcNow;

            _dbContext.Invitations.Update(invitation);

            // Guardar la actualización de la invitación
            var secondSaveResult = await _dbContext.SaveChangesAsync(cancellationToken);
            if (secondSaveResult <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to update invitation status after user creation");
                return new ApiResponse<bool>(false, "Failed to update invitation status", false);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "TaxUser registered successfully by invitation: {TaxUserId} for company {CompanyId}. "
                    + "Invitation {InvitationId} marked as accepted. "
                    + "Users: {Current}/{Limit}. Inherited permissions: {PermissionCount}",
                taxUser.Id,
                invitationData.CompanyId,
                invitationData.InvitationId,
                invitationData.CurrentActiveUserCount + 1, // +1 porque acabamos de crear uno
                invitationData.UserLimit,
                companyPermissionsToAdd.Count
            );

            // 13. Publicar evento de registro completado
            _eventBus.Publish(
                new UserRegisteredEvent(
                    Id: Guid.NewGuid(),
                    OccurredOn: DateTime.UtcNow,
                    TaxUserId: taxUser.Id,
                    Email: taxUser.Email,
                    Name: taxUser.Name,
                    LastName: taxUser.LastName,
                    CompanyId: invitationData.CompanyId,
                    CompanyName: invitationData.CompanyName,
                    CompanyFullName: invitationData.CompanyFullName,
                    CompanyDomain: invitationData.CompanyDomain,
                    IsCompany: invitationData.CompanyIsCompany
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
