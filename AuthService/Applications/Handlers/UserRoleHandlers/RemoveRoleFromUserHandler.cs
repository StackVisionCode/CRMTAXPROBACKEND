using Commands.UserRoleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserRoleHandlers;

public class RemoveRoleFromUserHandler
    : IRequestHandler<RemoveRoleFromUserCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RemoveRoleFromUserHandler> _log;

    public RemoveRoleFromUserHandler(
        ApplicationDbContext db,
        ILogger<RemoveRoleFromUserHandler> log
    )
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<bool>> Handle(RemoveRoleFromUserCommand req, CancellationToken ct)
    {
        try
        {
            // Buscar la relación con información de usuario y rol
            var relationshipQuery =
                from ur in _db.UserRoles
                join u in _db.TaxUsers on ur.TaxUserId equals u.Id
                join r in _db.Roles on ur.RoleId equals r.Id
                where ur.TaxUserId == req.UserId && ur.RoleId == req.RoleId
                select new
                {
                    UserRole = ur,
                    User = u,
                    Role = r,
                };

            var relationship = await relationshipQuery.FirstOrDefaultAsync(ct);
            if (relationship?.UserRole == null)
            {
                _log.LogWarning(
                    "Role assignment not found: User={UserId}, Role={RoleId}",
                    req.UserId,
                    req.RoleId
                );
                return new ApiResponse<bool>(false, "Role assignment not found", false);
            }

            // Si es Owner, verificar que no pierda todos los roles Administrator
            if (relationship.User.IsOwner && IsAdministratorRole(relationship.Role.Name))
            {
                // Contar otros roles Administrator que tiene el Owner
                var otherAdminRolesQuery =
                    from ur in _db.UserRoles
                    join r in _db.Roles on ur.RoleId equals r.Id
                    where
                        ur.TaxUserId == req.UserId
                        && ur.RoleId != req.RoleId
                        && (r.Name.Contains("Administrator") || r.Name == "Developer")
                    select ur.Id;

                var otherAdminRolesCount = await otherAdminRolesQuery.CountAsync(ct);

                if (otherAdminRolesCount == 0)
                {
                    _log.LogWarning(
                        "Cannot remove last Administrator role from company owner: User={UserId}, Role={RoleName}",
                        req.UserId,
                        relationship.Role.Name
                    );
                    return new ApiResponse<bool>(
                        false,
                        "Cannot remove the last Administrator role from company owner. Assign another Administrator role first.",
                        false
                    );
                }
            }

            // Remover la relación
            _db.UserRoles.Remove(relationship.UserRole);
            var success = await _db.SaveChangesAsync(ct) > 0;

            if (success)
            {
                _log.LogInformation(
                    "Role removed successfully: User={UserId} ({Email}, IsOwner={IsOwner}), Role={RoleName}",
                    req.UserId,
                    relationship.User.Email,
                    relationship.User.IsOwner,
                    relationship.Role.Name
                );
                return new ApiResponse<bool>(true, "Role removed successfully", true);
            }
            else
            {
                _log.LogError(
                    "Failed to remove role: User={UserId}, Role={RoleId}",
                    req.UserId,
                    req.RoleId
                );
                return new ApiResponse<bool>(false, "Failed to remove role", false);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error removing role: User={UserId}, Role={RoleId}, Message={Message}",
                req.UserId,
                req.RoleId,
                ex.Message
            );
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    /// <summary>
    /// Verificar si un rol es de tipo Administrator
    /// </summary>
    private static bool IsAdministratorRole(string roleName)
    {
        return roleName == "Developer" || roleName.Contains("Administrator");
    }
}
