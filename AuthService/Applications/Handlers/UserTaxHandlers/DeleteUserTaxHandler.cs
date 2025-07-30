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
            // ✅ MEJORADO: Buscar usuario con información completa
            var userQuery =
                from u in _dbContext.TaxUsers
                where u.Id == request.Id
                select new
                {
                    User = u,
                    CompanyId = u.CompanyId,
                    HasAddress = u.AddressId.HasValue,
                    AddressId = u.AddressId,
                    UserRoles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
                };

            var userData = await userQuery.FirstOrDefaultAsync(cancellationToken);
            if (userData?.User == null)
            {
                _logger.LogWarning("User not found: {UserId}", request.Id);
                return new ApiResponse<bool>(false, "User not found", false);
            }

            var user = userData.User;

            // ✅ NUEVO: Verificar si es el último administrador
            var isAdmin =
                userData.UserRoles.Contains("Administrator")
                || userData.UserRoles.Contains("Developer");

            if (isAdmin)
            {
                var otherAdminsCountQuery =
                    from u in _dbContext.TaxUsers
                    join ur in _dbContext.UserRoles on u.Id equals ur.TaxUserId
                    join r in _dbContext.Roles on ur.RoleId equals r.Id
                    where
                        u.CompanyId == userData.CompanyId
                        && u.Id != request.Id
                        && (r.Name == "Administrator" || r.Name == "Developer")
                    select u.Id;

                var otherAdminsCount = await otherAdminsCountQuery.CountAsync(cancellationToken);

                if (otherAdminsCount == 0)
                {
                    _logger.LogWarning(
                        "Cannot delete last administrator: UserId={UserId}, CompanyId={CompanyId}",
                        request.Id,
                        userData.CompanyId
                    );
                    return new ApiResponse<bool>(
                        false,
                        "Cannot delete the last administrator of the company. Assign another administrator first.",
                        false
                    );
                }
            }

            // ✅ MEJORADO: Obtener y eliminar sesiones del usuario
            var sessionsQuery =
                from s in _dbContext.Sessions
                where s.TaxUserId == request.Id
                select s;

            var sessions = await sessionsQuery.ToListAsync(cancellationToken);
            if (sessions.Any())
            {
                _dbContext.Sessions.RemoveRange(sessions);
                _logger.LogDebug(
                    "Marked {SessionCount} sessions for deletion for user: {UserId}",
                    sessions.Count,
                    request.Id
                );
            }

            // ✅ MEJORADO: Obtener y eliminar roles del usuario
            var userRolesQuery =
                from ur in _dbContext.UserRoles
                where ur.TaxUserId == request.Id
                select ur;

            var userRoles = await userRolesQuery.ToListAsync(cancellationToken);
            if (userRoles.Any())
            {
                _dbContext.UserRoles.RemoveRange(userRoles);
                _logger.LogDebug(
                    "Marked {RoleCount} user roles for deletion for user: {UserId}",
                    userRoles.Count,
                    request.Id
                );
            }

            // ✅ NUEVO: Eliminar dirección del usuario si existe
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
                    _logger.LogDebug("Marked user address for deletion: {AddressId}", address.Id);
                }
            }

            // ✅ Eliminar el usuario
            _dbContext.TaxUsers.Remove(user);

            // ✅ Guardar todos los cambios de una vez
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "User deleted successfully: UserId={UserId}, SessionsDeleted={SessionCount}, RolesDeleted={RoleCount}, AddressDeleted={AddressDeleted}",
                    request.Id,
                    sessions.Count,
                    userRoles.Count,
                    userData.HasAddress
                );
                return new ApiResponse<bool>(true, "User deleted successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to delete user: {UserId}", request.Id);
                return new ApiResponse<bool>(false, "Failed to delete user", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error deleting user: {UserId}", request.Id);
            return new ApiResponse<bool>(false, "An error occurred while deleting the user", false);
        }
    }
}
