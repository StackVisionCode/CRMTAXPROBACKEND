using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserTaxHandlers;

public class DeleteUserTaxHandler : IRequestHandler<DeleteTaxUserCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteUserTaxHandler> _logger;

    public DeleteUserTaxHandler(
        ApplicationDbContext dbContext,
        ILogger<DeleteUserTaxHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteTaxUserCommands request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Buscar usuario con información completa de company
            var userQuery =
                from u in _dbContext.TaxUsers
                join c in _dbContext.Companies on u.CompanyId equals c.Id
                where u.Id == request.UserId
                select new
                {
                    User = u,
                    Company = c,
                    HasAddress = u.AddressId.HasValue,
                    AddressId = u.AddressId,
                };

            var userData = await userQuery.FirstOrDefaultAsync(cancellationToken);
            if (userData?.User == null)
            {
                _logger.LogWarning("Tax user not found: {UserId}", request.UserId);
                return new ApiResponse<bool>(false, "Tax user not found", false);
            }

            var user = userData.User;

            // Verificar si es Owner
            if (user.IsOwner)
            {
                _logger.LogWarning(
                    "Cannot delete company owner: UserId={UserId}, Company={CompanyName}",
                    request.UserId,
                    userData.Company.IsCompany
                        ? userData.Company.CompanyName
                        : userData.Company.FullName
                );

                return new ApiResponse<bool>(
                    false,
                    "Cannot delete the company owner. Transfer ownership first or delete the entire company.",
                    false
                );
            }

            // Los Users regulares se pueden eliminar sin restricciones adicionales

            // Eliminar CompanyPermissions del usuario
            var companyPermissionsQuery =
                from cp in _dbContext.CompanyPermissions
                where cp.TaxUserId == request.UserId
                select cp;
            var companyPermissions = await companyPermissionsQuery.ToListAsync(cancellationToken);
            if (companyPermissions.Any())
            {
                _dbContext.CompanyPermissions.RemoveRange(companyPermissions);
                _logger.LogDebug(
                    "Marked {PermissionCount} company permissions for deletion for tax user: {UserId}",
                    companyPermissions.Count,
                    request.UserId
                );
            }

            // Eliminar sesiones del usuario
            var sessionsQuery =
                from s in _dbContext.Sessions
                where s.TaxUserId == request.UserId
                select s;
            var sessions = await sessionsQuery.ToListAsync(cancellationToken);
            if (sessions.Any())
            {
                _dbContext.Sessions.RemoveRange(sessions);
                _logger.LogDebug(
                    "Marked {SessionCount} sessions for deletion for tax user: {UserId}",
                    sessions.Count,
                    request.UserId
                );
            }

            // Eliminar roles del usuario
            var userRolesQuery =
                from ur in _dbContext.UserRoles
                where ur.TaxUserId == request.UserId
                select ur;
            var userRoles = await userRolesQuery.ToListAsync(cancellationToken);
            if (userRoles.Any())
            {
                _dbContext.UserRoles.RemoveRange(userRoles);
                _logger.LogDebug(
                    "Marked {RoleCount} user roles for deletion for tax user: {UserId}",
                    userRoles.Count,
                    request.UserId
                );
            }

            // Eliminar dirección del usuario si existe
            if (userData.HasAddress && userData.AddressId.HasValue)
            {
                var addressQuery =
                    from a in _dbContext.Addresses
                    where a.Id == userData.AddressId.Value
                    select a;
                var address = await addressQuery.FirstOrDefaultAsync(cancellationToken);
                if (address != null)
                {
                    _dbContext.Addresses.Remove(address);
                    _logger.LogDebug(
                        "Marked tax user address for deletion: {AddressId}",
                        address.Id
                    );
                }
            }

            // Eliminar el usuario
            _dbContext.TaxUsers.Remove(user);

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Tax user deleted successfully: UserId={UserId}, Company={CompanyName}, "
                        + "SessionsDeleted={SessionCount}, RolesDeleted={RoleCount}, PermissionsDeleted={PermissionCount}, AddressDeleted={AddressDeleted}",
                    request.UserId,
                    userData.Company.IsCompany
                        ? userData.Company.CompanyName
                        : userData.Company.FullName,
                    sessions.Count,
                    userRoles.Count,
                    companyPermissions.Count,
                    userData.HasAddress
                );

                return new ApiResponse<bool>(true, "Tax user deleted successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to delete tax user: {UserId}", request.UserId);
                return new ApiResponse<bool>(false, "Failed to delete tax user", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error deleting tax user: {UserId}", request.UserId);
            return new ApiResponse<bool>(
                false,
                "An error occurred while deleting the tax user",
                false
            );
        }
    }
}
