using AuthService.Domains.Users;
using Commands.UserRoleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserRoleHandlers;

public class UpdateUserRolesHandler : IRequestHandler<UpdateUserRolesCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<UpdateUserRolesHandler> _log;

    public UpdateUserRolesHandler(ApplicationDbContext db, ILogger<UpdateUserRolesHandler> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<bool>> Handle(UpdateUserRolesCommand req, CancellationToken ct)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            // Verificar que el usuario existe y obtener información importante
            var userInfoQuery =
                from u in _db.TaxUsers
                where u.Id == req.UserId
                select new { User = u, IsOwner = u.IsOwner };

            var userInfo = await userInfoQuery.FirstOrDefaultAsync(ct);
            if (userInfo?.User == null)
            {
                _log.LogWarning("User not found: {UserId}", req.UserId);
                return new ApiResponse<bool>(false, "User not found", false);
            }

            // VALIDACIÓN ESPECIAL PARA OWNER
            if (userInfo.IsOwner && req.RoleIds.Any())
            {
                // Verificar que el Owner mantenga al menos un rol Administrator
                var newRoleNamesQuery =
                    from r in _db.Roles
                    where req.RoleIds.Contains(r.Id)
                    select r.Name;

                var newRoleNames = await newRoleNamesQuery.ToListAsync(ct);
                var hasAdminRole = newRoleNames.Any(name =>
                    name.Contains("Administrator") || name == "Developer"
                );

                if (!hasAdminRole)
                {
                    _log.LogWarning(
                        "Owner must have at least one Administrator role: {UserId}",
                        req.UserId
                    );
                    return new ApiResponse<bool>(
                        false,
                        "Company owner must maintain at least one Administrator role",
                        false
                    );
                }
            }

            // Validar que todos los roles existen
            if (req.RoleIds.Any())
            {
                var validRolesQuery =
                    from r in _db.Roles
                    where req.RoleIds.Contains(r.Id)
                    select r.Id;

                var validRoles = await validRolesQuery.ToListAsync(ct);
                var distinctRoleIds = req.RoleIds.Distinct().ToList();

                if (validRoles.Count != distinctRoleIds.Count)
                {
                    var invalidIds = distinctRoleIds.Except(validRoles);
                    _log.LogWarning(
                        "Invalid role IDs for user {UserId}: {InvalidIds}",
                        req.UserId,
                        string.Join(", ", invalidIds)
                    );
                    return new ApiResponse<bool>(
                        false,
                        $"Invalid role IDs: {string.Join(", ", invalidIds)}",
                        false
                    );
                }
            }

            // Obtener roles actuales
            var currentRolesQuery =
                from ur in _db.UserRoles
                where ur.TaxUserId == req.UserId
                select ur;

            var currentRoles = await currentRolesQuery.ToListAsync(ct);
            var currentRoleIds = currentRoles.Select(ur => ur.RoleId).ToList();

            // Calcular cambios
            var newRoleIds = req.RoleIds.Distinct().ToList();
            var rolesToRemove = currentRoles.Where(ur => !newRoleIds.Contains(ur.RoleId)).ToList();
            var rolesToAdd = newRoleIds.Except(currentRoleIds).ToList();

            // Remover roles que ya no están
            if (rolesToRemove.Any())
            {
                _db.UserRoles.RemoveRange(rolesToRemove);
                _log.LogDebug(
                    "Removing {Count} roles from user {UserId}",
                    rolesToRemove.Count,
                    req.UserId
                );
            }

            // Agregar nuevos roles
            if (rolesToAdd.Any())
            {
                var newUserRoles = rolesToAdd
                    .Select(roleId => new UserRole
                    {
                        Id = Guid.NewGuid(),
                        TaxUserId = req.UserId,
                        RoleId = roleId,
                        CreatedAt = DateTime.UtcNow,
                    })
                    .ToList();

                await _db.UserRoles.AddRangeAsync(newUserRoles, ct);
                _log.LogDebug(
                    "Adding {Count} roles to user {UserId}",
                    rolesToAdd.Count,
                    req.UserId
                );
            }

            var result = await _db.SaveChangesAsync(ct) > 0;

            if (result)
            {
                await transaction.CommitAsync(ct);
                _log.LogInformation(
                    "User roles updated successfully: UserId={UserId}, IsOwner={IsOwner}, Removed={RemovedCount}, Added={AddedCount}, Total={TotalCount}",
                    req.UserId,
                    userInfo.IsOwner,
                    rolesToRemove.Count,
                    rolesToAdd.Count,
                    newRoleIds.Count
                );
                return new ApiResponse<bool>(true, "User roles updated successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(ct);
                _log.LogError("Failed to update user roles: {UserId}", req.UserId);
                return new ApiResponse<bool>(false, "Failed to update user roles", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _log.LogError(
                ex,
                "Error updating user roles for {UserId}: {Message}",
                req.UserId,
                ex.Message
            );
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
