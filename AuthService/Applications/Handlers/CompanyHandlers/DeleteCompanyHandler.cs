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
            // Análisis simplificado - solo datos de AuthService
            var companyAnalysisQuery =
                from c in _dbContext.Companies
                where c.Id == request.Id
                select new
                {
                    Company = c,
                    AddressId = c.AddressId,
                    TaxUsersCount = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id),
                    OwnerCount = _dbContext.TaxUsers.Count(u => u.CompanyId == c.Id && u.IsOwner),
                    RegularUsersCount = _dbContext.TaxUsers.Count(u =>
                        u.CompanyId == c.Id && !u.IsOwner
                    ),
                    ActiveSessionsCount = (
                        from s in _dbContext.Sessions
                        join u in _dbContext.TaxUsers on s.TaxUserId equals u.Id
                        where u.CompanyId == c.Id && !s.IsRevoke
                        select s.Id
                    ).Count(),
                    CompanyPermissionsCount = _dbContext.CompanyPermissions.Count(cp =>
                        _dbContext.TaxUsers.Any(u => u.Id == cp.TaxUserId && u.CompanyId == c.Id)
                    ),
                };

            var analysis = await companyAnalysisQuery.FirstOrDefaultAsync(cancellationToken);
            if (analysis?.Company == null)
            {
                _logger.LogWarning("Company not found: {CompanyId}", request.Id);
                return new ApiResponse<bool>(false, "Company not found", false);
            }

            var company = analysis.Company;

            _logger.LogInformation(
                "Analyzing company for deletion: {CompanyId}, ServiceLevel: {ServiceLevel}, "
                    + "TaxUsers: {TaxUsers} (Owner: {Owner}, Regular: {Regular}), ActiveSessions: {Sessions}, Permissions: {Permissions}",
                request.Id,
                company.ServiceLevel,
                analysis.TaxUsersCount,
                analysis.OwnerCount,
                analysis.RegularUsersCount,
                analysis.ActiveSessionsCount,
                analysis.CompanyPermissionsCount
            );

            // Validaciones para AuthService
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

            // IMPORTANTE: El frontend debe eliminar primero la suscripción en SubscriptionsService
            // Aquí solo eliminamos datos de AuthService

            // 1. Eliminar CompanyPermissions
            if (analysis.CompanyPermissionsCount > 0)
            {
                await DeleteCompanyPermissionsAsync(request.Id, cancellationToken);
            }

            // 2. Eliminar Sessions de TaxUsers
            if (analysis.ActiveSessionsCount > 0)
            {
                await DeleteTaxUserSessionsAsync(request.Id, cancellationToken);
            }

            // 3. Eliminar UserRoles
            await DeleteUserRolesAsync(request.Id, cancellationToken);

            // 4. Eliminar TaxUsers
            if (analysis.TaxUsersCount > 0)
            {
                await DeleteTaxUsersAsync(request.Id, cancellationToken);
            }

            // 5. Eliminar Address si existe
            if (analysis.AddressId.HasValue)
            {
                await DeleteAddressAsync(analysis.AddressId.Value, cancellationToken);
            }

            // 6. Eliminar la Company
            _dbContext.Companies.Remove(company);

            // Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Company deleted successfully: {CompanyId} with all AuthService dependencies "
                        + "(TaxUsers: {TaxUsers}, Address: {HasAddress})",
                    request.Id,
                    analysis.TaxUsersCount,
                    analysis.AddressId.HasValue
                );

                return new ApiResponse<bool>(
                    true,
                    "Company deleted successfully from AuthService",
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

    #region Validation and Deletion Methods

    private (bool CanDelete, string Reason) ValidateCompanyDeletion(dynamic analysis)
    {
        var totalUsers = analysis.TaxUsersCount;
        if (totalUsers > 1)
        {
            return (
                false,
                $"Cannot delete company with {totalUsers} users. Only companies with 1 or no users can be deleted."
            );
        }

        var activeSessions = analysis.ActiveSessionsCount;
        if (activeSessions > 0)
        {
            return (
                false,
                $"Cannot delete company with {activeSessions} active sessions. Please wait for sessions to expire or revoke them first."
            );
        }

        // El frontend debe validar que no hay suscripción activa en SubscriptionsService
        return (true, "Company can be safely deleted from AuthService");
    }

    private async Task DeleteCompanyPermissionsAsync(
        Guid companyId,
        CancellationToken cancellationToken
    )
    {
        var permissionsQuery =
            from cp in _dbContext.CompanyPermissions
            join u in _dbContext.TaxUsers on cp.TaxUserId equals u.Id
            where u.CompanyId == companyId
            select cp;

        var permissions = await permissionsQuery.ToListAsync(cancellationToken);
        if (permissions.Any())
        {
            _dbContext.CompanyPermissions.RemoveRange(permissions);
            _logger.LogDebug("Marked {Count} company permissions for deletion", permissions.Count);
        }
    }

    private async Task DeleteTaxUserSessionsAsync(
        Guid companyId,
        CancellationToken cancellationToken
    )
    {
        var sessionsQuery =
            from s in _dbContext.Sessions
            join u in _dbContext.TaxUsers on s.TaxUserId equals u.Id
            where u.CompanyId == companyId
            select s;

        var sessions = await sessionsQuery.ToListAsync(cancellationToken);
        if (sessions.Any())
        {
            _dbContext.Sessions.RemoveRange(sessions);
            _logger.LogDebug("Marked {Count} tax user sessions for deletion", sessions.Count);
        }
    }

    private async Task DeleteUserRolesAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var rolesQuery =
            from ur in _dbContext.UserRoles
            join u in _dbContext.TaxUsers on ur.TaxUserId equals u.Id
            where u.CompanyId == companyId
            select ur;

        var roles = await rolesQuery.ToListAsync(cancellationToken);
        if (roles.Any())
        {
            _dbContext.UserRoles.RemoveRange(roles);
            _logger.LogDebug("Marked {Count} user roles for deletion", roles.Count);
        }
    }

    private async Task DeleteTaxUsersAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var users = await _dbContext
            .TaxUsers.Where(u => u.CompanyId == companyId)
            .ToListAsync(cancellationToken);

        if (users.Any())
        {
            _dbContext.TaxUsers.RemoveRange(users);
            _logger.LogDebug("Marked {Count} tax users for deletion", users.Count);
        }
    }

    private async Task DeleteAddressAsync(Guid addressId, CancellationToken cancellationToken)
    {
        var address = await _dbContext.Addresses.FirstOrDefaultAsync(
            a => a.Id == addressId,
            cancellationToken
        );

        if (address != null)
        {
            _dbContext.Addresses.Remove(address);
            _logger.LogDebug("Marked address for deletion: {AddressId}", addressId);
        }
    }

    #endregion
}
