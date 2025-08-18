using AuthService.Domains.Users;
using Commands.UserRoleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserRoleHandlers;

public class AssignRoleToUserHandler : IRequestHandler<AssignRoleToUserCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AssignRoleToUserHandler> _log;

    public AssignRoleToUserHandler(ApplicationDbContext db, ILogger<AssignRoleToUserHandler> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<bool>> Handle(AssignRoleToUserCommand req, CancellationToken ct)
    {
        try
        {
            // Validar existencia con información adicional
            var userInfoQuery =
                from u in _db.TaxUsers
                where u.Id == req.UserId
                select new
                {
                    u.Id,
                    u.Email,
                    u.IsOwner,
                    u.CompanyId,
                };

            var userInfo = await userInfoQuery.FirstOrDefaultAsync(ct);
            if (userInfo == null)
            {
                _log.LogWarning("User not found: {UserId}", req.UserId);
                return new ApiResponse<bool>(false, "User not found", false);
            }

            var roleInfoQuery =
                from r in _db.Roles
                where r.Id == req.RoleId
                select new
                {
                    r.Id,
                    r.Name,
                    r.ServiceLevel,
                    r.PortalAccess,
                };

            var roleInfo = await roleInfoQuery.FirstOrDefaultAsync(ct);
            if (roleInfo == null)
            {
                _log.LogWarning("Role not found: {RoleId}", req.RoleId);
                return new ApiResponse<bool>(false, "Role not found", false);
            }

            // Verificar si ya tiene el rol asignado
            var alreadyAssignedQuery =
                from ur in _db.UserRoles
                where ur.TaxUserId == req.UserId && ur.RoleId == req.RoleId
                select ur.Id;

            var alreadyAssigned = await alreadyAssignedQuery.AnyAsync(ct);
            if (alreadyAssigned)
            {
                _log.LogWarning(
                    "User {UserId} already has role {RoleName}",
                    req.UserId,
                    roleInfo.Name
                );
                return new ApiResponse<bool>(false, "User already has this role", false);
            }

            // Verificar compatibilidad Owner-Role
            if (userInfo.IsOwner && !IsOwnerCompatibleRole(roleInfo.Name))
            {
                _log.LogWarning(
                    "Cannot assign incompatible role {RoleName} to company owner {UserId}",
                    roleInfo.Name,
                    req.UserId
                );
                return new ApiResponse<bool>(
                    false,
                    "Cannot assign User or Customer roles to company owner",
                    false
                );
            }

            // Verificar que Users regulares no reciban roles Administrator
            if (!userInfo.IsOwner && IsAdministratorRole(roleInfo.Name))
            {
                _log.LogWarning(
                    "Cannot assign Administrator role {RoleName} to regular user {UserId}",
                    roleInfo.Name,
                    req.UserId
                );
                return new ApiResponse<bool>(
                    false,
                    "Cannot assign Administrator roles to regular users",
                    false
                );
            }

            // Crear la asignación
            var userRole = new UserRole
            {
                Id = Guid.NewGuid(),
                TaxUserId = req.UserId,
                RoleId = req.RoleId,
                CreatedAt = DateTime.UtcNow,
            };

            await _db.UserRoles.AddAsync(userRole, ct);
            var success = await _db.SaveChangesAsync(ct) > 0;

            if (success)
            {
                _log.LogInformation(
                    "Role assigned successfully: User={UserId} ({Email}, IsOwner={IsOwner}), Role={RoleName}",
                    req.UserId,
                    userInfo.Email,
                    userInfo.IsOwner,
                    roleInfo.Name
                );
                return new ApiResponse<bool>(true, "Role assigned successfully", true);
            }
            else
            {
                _log.LogError(
                    "Failed to assign role: User={UserId}, Role={RoleId}",
                    req.UserId,
                    req.RoleId
                );
                return new ApiResponse<bool>(false, "Failed to assign role", false);
            }
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error assigning role: User={UserId}, Role={RoleId}, Message={Message}",
                req.UserId,
                req.RoleId,
                ex.Message
            );
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }

    /// <summary>
    /// Verificar si un rol es compatible con Owner
    /// </summary>
    private static bool IsOwnerCompatibleRole(string roleName)
    {
        return roleName == "Developer" || roleName.Contains("Administrator");
    }

    /// <summary>
    /// Verificar si un rol es de tipo Administrator
    /// </summary>
    private static bool IsAdministratorRole(string roleName)
    {
        return roleName == "Developer" || roleName.Contains("Administrator");
    }
}
