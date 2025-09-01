using Applications.DTOs.AddressDTOs;
using AuthService.DTOs.UserDTOs;
using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserHandlers;

public class TransferUserHandler : IRequestHandler<TransferUserCommand, ApiResponse<UserGetDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TransferUserHandler> _logger;

    public TransferUserHandler(ApplicationDbContext dbContext, ILogger<TransferUserHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<UserGetDTO>> Handle(
        TransferUserCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Buscar TaxUser origen
            var sourceUserQuery =
                from u in _dbContext.TaxUsers
                join c in _dbContext.Companies on u.CompanyId equals c.Id
                where u.Id == request.UserId
                select new { User = u, SourceCompany = c };

            var sourceData = await sourceUserQuery.FirstOrDefaultAsync(cancellationToken);
            if (sourceData?.User == null)
            {
                return new ApiResponse<UserGetDTO>(false, "User not found", null!);
            }

            // 2. Verificar company destino (sin CustomPlan)
            var targetCompanyQuery =
                from c in _dbContext.Companies
                where c.Id == request.TargetCompanyId
                select new
                {
                    Company = c,
                    CurrentActiveUserCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && u.IsActive
                    ),
                };

            var targetData = await targetCompanyQuery.FirstOrDefaultAsync(cancellationToken);
            if (targetData?.Company == null)
            {
                return new ApiResponse<UserGetDTO>(false, "Target company not found", null!);
            }

            // 3. VALIDACIÓN DE LÍMITES SIMPLIFICADA
            // El frontend debe haber validado límites consultando SubscriptionsService
            // Aquí solo logueamos para auditoría
            _logger.LogInformation(
                "Transferring user {UserId} from company {SourceCompanyId} (ServiceLevel: {SourceLevel}) to {TargetCompanyId} (ServiceLevel: {TargetLevel}). Target active users: {TargetUsers}",
                request.UserId,
                sourceData.SourceCompany.Id,
                sourceData.SourceCompany.ServiceLevel,
                targetData.Company.Id,
                targetData.Company.ServiceLevel,
                targetData.CurrentActiveUserCount
            );

            var user = sourceData.User;

            // 4. Actualizar CompanyId
            user.CompanyId = request.TargetCompanyId;
            user.UpdatedAt = DateTime.UtcNow;

            // 5. Revocar sesiones existentes (cambio de company requiere re-login)
            var activeSessions = await _dbContext
                .Sessions.Where(s => s.TaxUserId == request.UserId && !s.IsRevoke)
                .ToListAsync(cancellationToken);

            foreach (var session in activeSessions)
            {
                session.IsRevoke = true;
                session.UpdatedAt = DateTime.UtcNow;
            }

            // 6. Limpiar permisos personalizados (nueva company = nuevos permisos)
            await ClearUserCustomPermissionsAsync(request.UserId, cancellationToken);

            // 7. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<UserGetDTO>(false, "Failed to transfer user", null!);
            }

            await transaction.CommitAsync(cancellationToken);

            // 8. Obtener TaxUser actualizado usando JOINs potentes
            var userDto = await GetTransferredUserWithJoinsAsync(user.Id, cancellationToken);

            if (userDto == null)
            {
                _logger.LogError("Failed to retrieve transferred user: {UserId}", user.Id);
                return new ApiResponse<UserGetDTO>(
                    false,
                    "User transferred but failed to retrieve",
                    null!
                );
            }

            _logger.LogInformation(
                "User transferred successfully: {UserId} from {SourceCompany} (ServiceLevel: {SourceLevel}) to {TargetCompany} (ServiceLevel: {TargetLevel})",
                request.UserId,
                sourceData.SourceCompany.CompanyName ?? sourceData.SourceCompany.FullName,
                sourceData.SourceCompany.ServiceLevel,
                targetData.Company.CompanyName ?? targetData.Company.FullName,
                targetData.Company.ServiceLevel
            );

            return new ApiResponse<UserGetDTO>(true, "User transferred successfully", userDto);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error transferring user: {UserId}", request.UserId);
            return new ApiResponse<UserGetDTO>(false, "Error transferring user", null!);
        }
    }

    #region Helper Methods

    private async Task<UserGetDTO?> GetTransferredUserWithJoinsAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        // JOIN potente para obtener usuario transferido
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

                // Información de la company (nueva)
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

    private async Task ClearUserCustomPermissionsAsync(
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        var existingPermissions = await _dbContext
            .CompanyPermissions.Where(cp => cp.TaxUserId == userId)
            .ToListAsync(cancellationToken);

        if (existingPermissions.Any())
        {
            _dbContext.CompanyPermissions.RemoveRange(existingPermissions);
            _logger.LogDebug(
                "Cleared {Count} custom permissions for transferred user {UserId}",
                existingPermissions.Count,
                userId
            );
        }
    }

    #endregion
}
