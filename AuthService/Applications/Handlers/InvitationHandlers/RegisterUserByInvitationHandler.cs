using AuthService.Applications.Common;
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

            // 1. Validar token de invitación (sin CustomPlans)
            var invitationQuery =
                from i in _dbContext.Invitations
                join c in _dbContext.Companies on i.CompanyId equals c.Id
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
                    CompanyServiceLevel = c.ServiceLevel, // NUEVO
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
                // Obtener la invitación para actualizarla
                var expiredInvitation = await _dbContext.Invitations.FirstOrDefaultAsync(
                    i => i.Id == invitationData.InvitationId,
                    cancellationToken
                );

                if (expiredInvitation != null)
                {
                    expiredInvitation.Status = InvitationStatus.Expired;
                    expiredInvitation.UpdatedAt = DateTime.UtcNow;

                    // Guardar los cambios
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }

                _logger.LogWarning(
                    "Invitation has expired and was marked as expired: {InvitationId}",
                    invitationData.InvitationId
                );
                return new ApiResponse<bool>(false, "Invitation has expired", false);
            }

            // 3. VALIDACIÓN DE LÍMITES SIMPLIFICADA
            // El frontend debe haber validado límites consultando SubscriptionsService
            // Aquí solo validamos reglas básicas de AuthService
            if (invitationData.OwnerCount == 0)
            {
                _logger.LogError(
                    "Company {CompanyId} has no active Owner",
                    invitationData.CompanyId
                );
                return new ApiResponse<bool>(false, "Company has no active administrator", false);
            }

            _logger.LogInformation(
                "Processing invitation registration for company {CompanyId} (ServiceLevel: {ServiceLevel}). Current users: {CurrentUsers}",
                invitationData.CompanyId,
                invitationData.CompanyServiceLevel,
                invitationData.CurrentActiveUserCount
            );

            // 4. Verificar que el email no esté registrado
            var emailExists = await _dbContext.TaxUsers.AnyAsync(
                u => u.Email == invitationData.Email,
                cancellationToken
            );

            if (emailExists)
            {
                _logger.LogWarning("Email already registered: {Email}", invitationData.Email);
                return new ApiResponse<bool>(false, "Email already registered", false);
            }

            // 5. Validar roles para el ServiceLevel de la company
            if (invitationData.RoleIds?.Any() == true)
            {
                var roleValidation = await ValidateRolesForServiceLevelAsync(
                    invitationData.RoleIds,
                    invitationData.CompanyServiceLevel,
                    cancellationToken
                );

                if (!roleValidation.IsValid)
                {
                    return new ApiResponse<bool>(false, roleValidation.Message, false);
                }
            }

            // 6. Obtener permisos del Administrator para heredar
            var adminPermissions = await GetAdminPermissionsToInheritAsync(
                invitationData.CompanyId,
                cancellationToken
            );

            // 7. Crear dirección si se proporciona
            Address? userAddressEntity = null;
            if (registration.Address is { } addressDto)
            {
                var validateResult = await ValidateAddressAsync(
                    addressDto.CountryId,
                    addressDto.StateId,
                    cancellationToken
                );
                if (!validateResult.Success)
                {
                    return new ApiResponse<bool>(false, validateResult.Message, false);
                }

                userAddressEntity = new Address
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

                await _dbContext.Addresses.AddAsync(userAddressEntity, cancellationToken);
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
            await AssignRolesToUserAsync(
                taxUser.Id,
                invitationData.RoleIds,
                invitationData.CompanyServiceLevel,
                cancellationToken
            );

            // 10. Heredar permisos del Administrator
            await InheritAdminPermissionsAsync(taxUser.Id, adminPermissions, cancellationToken);

            // 11. Guardar TaxUser y dependencias
            var firstSaveResult = await _dbContext.SaveChangesAsync(cancellationToken);
            if (firstSaveResult <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to create TaxUser by invitation - first save failed");
                return new ApiResponse<bool>(false, "Failed to create user account", false);
            }

            _logger.LogInformation("TaxUser created successfully with ID: {TaxUserId}", taxUser.Id);

            // 12. Actualizar invitación
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

            invitation.Status = InvitationStatus.Accepted;
            invitation.AcceptedAt = DateTime.UtcNow;
            invitation.RegisteredUserId = taxUser.Id;
            invitation.UpdatedAt = DateTime.UtcNow;

            _dbContext.Invitations.Update(invitation);

            var secondSaveResult = await _dbContext.SaveChangesAsync(cancellationToken);
            if (secondSaveResult <= 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to update invitation status after user creation");
                return new ApiResponse<bool>(false, "Failed to update invitation status", false);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "TaxUser registered successfully by invitation: {TaxUserId} for company {CompanyId} (ServiceLevel: {ServiceLevel}). "
                    + "Invitation {InvitationId} marked as accepted. Current users: {CurrentUsers}. Inherited permissions: {PermissionCount}",
                taxUser.Id,
                invitationData.CompanyId,
                invitationData.CompanyServiceLevel,
                invitationData.InvitationId,
                invitationData.CurrentActiveUserCount + 1,
                adminPermissions.Count
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

    #region Helper Methods

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
            // No permitir roles de Administrator o Developer en invitaciones
            if (role.Name.Contains("Administrator") || role.Name.Contains("Developer"))
            {
                return (false, $"Role '{role.Name}' cannot be assigned via invitation");
            }

            // Validar ServiceLevel
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

    private async Task<
        List<(Guid PermissionId, string Code, string Name)>
    > GetAdminPermissionsToInheritAsync(Guid companyId, CancellationToken cancellationToken)
    {
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
            companyId
        );

        return adminPermissions
            .Select(p => (p.PermissionId, p.PermissionCode, p.PermissionName))
            .ToList();
    }

    private async Task AssignRolesToUserAsync(
        Guid userId,
        ICollection<Guid>? roleIds,
        ServiceLevel companyServiceLevel,
        CancellationToken cancellationToken
    )
    {
        var rolesToAssign = new List<Guid>();

        if (roleIds?.Any() == true)
        {
            // Validar que los roles especificados existen y son apropiados
            var validRoles = await _dbContext
                .Roles.Where(r =>
                    roleIds.Contains(r.Id)
                    && !r.Name.Contains("Administrator")
                    && !r.Name.Contains("Developer")
                    && (!r.ServiceLevel.HasValue || r.ServiceLevel <= companyServiceLevel)
                )
                .Select(r => r.Id)
                .ToListAsync(cancellationToken);

            rolesToAssign.AddRange(validRoles);
        }

        // Si no hay roles válidos, asignar User por defecto
        if (!rolesToAssign.Any())
        {
            var userRoleId = await _dbContext
                .Roles.Where(r => r.Name == "User")
                .Select(r => r.Id)
                .FirstOrDefaultAsync(cancellationToken);

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
                TaxUserId = userId,
                RoleId = roleId,
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.UserRoles.AddAsync(userRole, cancellationToken);
        }
    }

    private async Task InheritAdminPermissionsAsync(
        Guid userId,
        List<(Guid PermissionId, string Code, string Name)> adminPermissions,
        CancellationToken cancellationToken
    )
    {
        var companyPermissionsToAdd = new List<CompanyPermission>();

        foreach (var (permissionId, code, name) in adminPermissions)
        {
            var companyPermission = new CompanyPermission
            {
                Id = Guid.NewGuid(),
                TaxUserId = userId,
                PermissionId = permissionId,
                IsGranted = true,
                Description = $"Inherited from Administrator role on registration - {code}",
                CreatedAt = DateTime.UtcNow,
            };

            companyPermissionsToAdd.Add(companyPermission);
        }

        if (companyPermissionsToAdd.Any())
        {
            await _dbContext.CompanyPermissions.AddRangeAsync(
                companyPermissionsToAdd,
                cancellationToken
            );

            _logger.LogInformation(
                "Inherited {PermissionCount} administrator permissions for new user {UserId}",
                companyPermissionsToAdd.Count,
                userId
            );
        }
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
