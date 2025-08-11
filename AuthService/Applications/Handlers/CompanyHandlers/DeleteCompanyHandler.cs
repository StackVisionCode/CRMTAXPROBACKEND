using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.CompanyHandlers;

public class DeleteCompanyHandler : IRequestHandler<DeleteCompanyCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteCompanyHandler> _logger;

    public DeleteCompanyHandler(
        ApplicationDbContext dbContext,
        ILogger<DeleteCompanyHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteCompanyCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Buscar company con información completa de dependencias
            var companyAnalysisQuery =
                from c in _dbContext.Companies
                where c.Id == request.Id
                select new
                {
                    Company = c,
                    CustomPlanId = c.CustomPlanId,
                    AddressId = c.AddressId,
                    // Conteo de dependencias críticas
                    TaxUsersCount = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id),
                    UserCompaniesCount = _dbContext.UserCompanies.Count(uc => uc.CompanyId == c.Id),
                    // Sesiones activas
                    TaxUserSessionsCount = (
                        from s in _dbContext.Sessions
                        join u in _dbContext.TaxUsers on s.TaxUserId equals u.Id
                        where u.CompanyId == c.Id && !s.IsRevoke
                        select s.Id
                    ).Count(),
                    UserCompanySessionsCount = (
                        from ucs in _dbContext.UserCompanySessions
                        join uc in _dbContext.UserCompanies on ucs.UserCompanyId equals uc.Id
                        where uc.CompanyId == c.Id && !ucs.IsRevoke
                        select ucs.Id
                    ).Count(),
                    // Permisos personalizados
                    CompanyPermissionsCount = (
                        from cp in _dbContext.CompanyPermissions
                        join uc in _dbContext.UserCompanies on cp.UserCompanyId equals uc.Id
                        where uc.CompanyId == c.Id
                        select cp.Id
                    ).Count(),
                    // Módulos personalizados
                    CustomModulesCount = (
                        from cm in _dbContext.CustomModules
                        join cplan in _dbContext.CustomPlans on cm.CustomPlanId equals cplan.Id
                        where cplan.CompanyId == c.Id
                        select cm.Id
                    ).Count(),
                };

            var analysis = await companyAnalysisQuery.FirstOrDefaultAsync(cancellationToken);
            if (analysis?.Company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", request.Id);
                return new ApiResponse<bool>(false, "Company not found", false);
            }

            var company = analysis.Company;

            _logger.LogInformation(
                "Analyzing company for deletion: {CompanyId}, TaxUsers: {TaxUsers}, UserCompanies: {UserCompanies}, "
                    + "ActiveSessions: {TaxSessions}+{UserSessions}, Permissions: {Permissions}, Modules: {Modules}",
                request.Id,
                analysis.TaxUsersCount,
                analysis.UserCompaniesCount,
                analysis.TaxUserSessionsCount,
                analysis.UserCompanySessionsCount,
                analysis.CompanyPermissionsCount,
                analysis.CustomModulesCount
            );

            // Validaciones exhaustivas de dependencias
            var validationResult = ValidateCompanyDeletion(analysis);
            if (!validationResult.CanDelete)
            {
                _logger.LogWarning(
                    "Cannot delete company {CompanyId}: {Reason}",
                    request.Id,
                    validationResult.Reason
                );
                return new ApiResponse<bool>(false, validationResult.Reason, false);
            }

            // Eliminar en orden correcto para evitar violaciones de FK

            // 1. Eliminar CompanyPermissions (dependen de UserCompanies)
            if (analysis.CompanyPermissionsCount > 0)
            {
                await DeleteCompanyPermissionsAsync(request.Id, cancellationToken);
            }

            // 2. Eliminar UserCompanySessions (dependen de UserCompanies)
            if (analysis.UserCompanySessionsCount > 0)
            {
                await DeleteUserCompanySessionsAsync(request.Id, cancellationToken);
            }

            // 3. Eliminar UserCompanyRoles (dependen de UserCompanies)
            await DeleteUserCompanyRolesAsync(request.Id, cancellationToken);

            // 4. Eliminar UserCompanies
            if (analysis.UserCompaniesCount > 0)
            {
                await DeleteUserCompaniesAsync(request.Id, cancellationToken);
            }

            // 5. Eliminar Sessions de TaxUsers (dependen de TaxUsers)
            if (analysis.TaxUserSessionsCount > 0)
            {
                await DeleteTaxUserSessionsAsync(request.Id, cancellationToken);
            }

            // 6. Eliminar UserRoles (dependen de TaxUsers)
            await DeleteUserRolesAsync(request.Id, cancellationToken);

            // 7. Eliminar TaxUsers
            if (analysis.TaxUsersCount > 0)
            {
                await DeleteTaxUsersAsync(request.Id, cancellationToken);
            }

            // 8. Eliminar CustomModules (dependen de CustomPlan)
            if (analysis.CustomModulesCount > 0)
            {
                await DeleteCustomModulesAsync(analysis.CustomPlanId, cancellationToken);
            }

            // 9. Eliminar CustomPlan
            await DeleteCustomPlanAsync(analysis.CustomPlanId, cancellationToken);

            // 10. Eliminar Address si existe
            if (analysis.AddressId.HasValue)
            {
                await DeleteAddressAsync(analysis.AddressId.Value, cancellationToken);
            }

            // 11. Finalmente eliminar la Company
            _dbContext.Companies.Remove(company);

            // Guardar todos los cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Company deleted successfully: {CompanyId} with all dependencies "
                        + "(Users: {TaxUsers}+{UserCompanies}, Modules: {Modules}, Address: {HasAddress})",
                    request.Id,
                    analysis.TaxUsersCount,
                    analysis.UserCompaniesCount,
                    analysis.CustomModulesCount,
                    analysis.AddressId.HasValue
                );

                return new ApiResponse<bool>(
                    true,
                    "Company and all related data deleted successfully",
                    true
                );
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to delete company: {CompanyId}", request.Id);
                return new ApiResponse<bool>(false, "Failed to delete company", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error deleting company {CompanyId}: {Message}",
                request.Id,
                ex.Message
            );
            return new ApiResponse<bool>(
                false,
                "An error occurred while deleting the company",
                false
            );
        }
    }

    /// <summary>
    /// Valida si la company puede ser eliminada
    /// </summary>
    private (bool CanDelete, string Reason) ValidateCompanyDeletion(dynamic analysis)
    {
        // Verificar usuarios activos
        var totalUsers = analysis.TaxUsersCount + analysis.UserCompaniesCount;
        if (totalUsers > 1) // Más que solo el admin
        {
            return (
                false,
                $"Cannot delete company with {totalUsers} users. Only companies with 1 or no users can be deleted."
            );
        }

        // Verificar sesiones activas
        var activeSessions = analysis.TaxUserSessionsCount + analysis.UserCompanySessionsCount;
        if (activeSessions > 0)
        {
            return (
                false,
                $"Cannot delete company with {activeSessions} active sessions. Please wait for sessions to expire or revoke them first."
            );
        }

        // TODO: Agregar más validaciones según reglas de negocio
        // Ejemplo: verificar si hay facturas pendientes, reportes en proceso, etc.

        return (true, "Company can be safely deleted");
    }

    /// <summary>
    /// Elimina permisos personalizados de la company
    /// </summary>
    private async Task DeleteCompanyPermissionsAsync(Guid companyId, CancellationToken ct)
    {
        var permissionsQuery =
            from cp in _dbContext.CompanyPermissions
            join uc in _dbContext.UserCompanies on cp.UserCompanyId equals uc.Id
            where uc.CompanyId == companyId
            select cp;

        var permissions = await permissionsQuery.ToListAsync(ct);
        if (permissions.Any())
        {
            _dbContext.CompanyPermissions.RemoveRange(permissions);
            _logger.LogDebug("Marked {Count} company permissions for deletion", permissions.Count);
        }
    }

    /// <summary>
    /// Elimina sesiones de UserCompanies
    /// </summary>
    private async Task DeleteUserCompanySessionsAsync(Guid companyId, CancellationToken ct)
    {
        var sessionsQuery =
            from ucs in _dbContext.UserCompanySessions
            join uc in _dbContext.UserCompanies on ucs.UserCompanyId equals uc.Id
            where uc.CompanyId == companyId
            select ucs;

        var sessions = await sessionsQuery.ToListAsync(ct);
        if (sessions.Any())
        {
            _dbContext.UserCompanySessions.RemoveRange(sessions);
            _logger.LogDebug("Marked {Count} user company sessions for deletion", sessions.Count);
        }
    }

    /// <summary>
    /// Elimina roles de UserCompanies
    /// </summary>
    private async Task DeleteUserCompanyRolesAsync(Guid companyId, CancellationToken ct)
    {
        var rolesQuery =
            from ucr in _dbContext.UserCompanyRoles
            join uc in _dbContext.UserCompanies on ucr.UserCompanyId equals uc.Id
            where uc.CompanyId == companyId
            select ucr;

        var roles = await rolesQuery.ToListAsync(ct);
        if (roles.Any())
        {
            _dbContext.UserCompanyRoles.RemoveRange(roles);
            _logger.LogDebug("Marked {Count} user company roles for deletion", roles.Count);
        }
    }

    /// <summary>
    /// Elimina UserCompanies
    /// </summary>
    private async Task DeleteUserCompaniesAsync(Guid companyId, CancellationToken ct)
    {
        var userCompaniesQuery =
            from uc in _dbContext.UserCompanies
            where uc.CompanyId == companyId
            select uc;

        var userCompanies = await userCompaniesQuery.ToListAsync(ct);
        if (userCompanies.Any())
        {
            _dbContext.UserCompanies.RemoveRange(userCompanies);
            _logger.LogDebug("Marked {Count} user companies for deletion", userCompanies.Count);
        }
    }

    /// <summary>
    /// Elimina sesiones de TaxUsers
    /// </summary>
    private async Task DeleteTaxUserSessionsAsync(Guid companyId, CancellationToken ct)
    {
        var sessionsQuery =
            from s in _dbContext.Sessions
            join u in _dbContext.TaxUsers on s.TaxUserId equals u.Id
            where u.CompanyId == companyId
            select s;

        var sessions = await sessionsQuery.ToListAsync(ct);
        if (sessions.Any())
        {
            _dbContext.Sessions.RemoveRange(sessions);
            _logger.LogDebug("Marked {Count} tax user sessions for deletion", sessions.Count);
        }
    }

    /// <summary>
    /// Elimina roles de TaxUsers
    /// </summary>
    private async Task DeleteUserRolesAsync(Guid companyId, CancellationToken ct)
    {
        var rolesQuery =
            from ur in _dbContext.UserRoles
            join u in _dbContext.TaxUsers on ur.TaxUserId equals u.Id
            where u.CompanyId == companyId
            select ur;

        var roles = await rolesQuery.ToListAsync(ct);
        if (roles.Any())
        {
            _dbContext.UserRoles.RemoveRange(roles);
            _logger.LogDebug("Marked {Count} user roles for deletion", roles.Count);
        }
    }

    /// <summary>
    /// Elimina TaxUsers
    /// </summary>
    private async Task DeleteTaxUsersAsync(Guid companyId, CancellationToken ct)
    {
        var usersQuery = from u in _dbContext.TaxUsers where u.CompanyId == companyId select u;

        var users = await usersQuery.ToListAsync(ct);
        if (users.Any())
        {
            _dbContext.TaxUsers.RemoveRange(users);
            _logger.LogDebug("Marked {Count} tax users for deletion", users.Count);
        }
    }

    /// <summary>
    /// Elimina módulos personalizados del CustomPlan
    /// </summary>
    private async Task DeleteCustomModulesAsync(Guid customPlanId, CancellationToken ct)
    {
        var modulesQuery =
            from cm in _dbContext.CustomModules
            where cm.CustomPlanId == customPlanId
            select cm;

        var modules = await modulesQuery.ToListAsync(ct);
        if (modules.Any())
        {
            _dbContext.CustomModules.RemoveRange(modules);
            _logger.LogDebug("Marked {Count} custom modules for deletion", modules.Count);
        }
    }

    /// <summary>
    /// Elimina el CustomPlan
    /// </summary>
    private async Task DeleteCustomPlanAsync(Guid customPlanId, CancellationToken ct)
    {
        var planQuery = from cp in _dbContext.CustomPlans where cp.Id == customPlanId select cp;

        var customPlan = await planQuery.FirstOrDefaultAsync(ct);
        if (customPlan != null)
        {
            _dbContext.CustomPlans.Remove(customPlan);
            _logger.LogDebug("Marked custom plan for deletion: {CustomPlanId}", customPlanId);
        }
    }

    /// <summary>
    /// Elimina la dirección si existe
    /// </summary>
    private async Task DeleteAddressAsync(Guid addressId, CancellationToken ct)
    {
        var addressQuery = from a in _dbContext.Addresses where a.Id == addressId select a;

        var address = await addressQuery.FirstOrDefaultAsync(ct);
        if (address != null)
        {
            _dbContext.Addresses.Remove(address);
            _logger.LogDebug("Marked address for deletion: {AddressId}", addressId);
        }
    }
}
