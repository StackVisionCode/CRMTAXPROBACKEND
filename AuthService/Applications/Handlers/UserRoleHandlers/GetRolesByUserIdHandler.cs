using AuthService.DTOs.RoleDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.UserRoleQueries;

namespace Handlers.UserRoleHandlers;

public class GetRolesByUserIdHandler
    : IRequestHandler<GetRolesByUserIdQuery, ApiResponse<List<RoleDTO>>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GetRolesByUserIdHandler> _log;

    public GetRolesByUserIdHandler(ApplicationDbContext db, ILogger<GetRolesByUserIdHandler> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<List<RoleDTO>>> Handle(
        GetRolesByUserIdQuery req,
        CancellationToken ct
    )
    {
        try
        {
            // Verificar que el usuario existe
            var userExistsQuery = from u in _db.TaxUsers where u.Id == req.UserId select u.Id;

            var userExists = await userExistsQuery.AnyAsync(ct);
            if (!userExists)
            {
                _log.LogWarning("User not found: {UserId}", req.UserId);
                return new ApiResponse<List<RoleDTO>>(false, "User not found", new List<RoleDTO>());
            }

            // Obtener roles del usuario con sus permisos
            var rolesData = await (
                from ur in _db.UserRoles
                where ur.TaxUserId == req.UserId
                join r in _db.Roles on ur.RoleId equals r.Id
                select new
                {
                    r.Id,
                    r.Name,
                    r.Description,
                    r.PortalAccess,
                }
            ).Distinct().ToListAsync(ct);

            if (!rolesData.Any())
            {
                _log.LogInformation("User {UserId} has no roles assigned", req.UserId);
                return new ApiResponse<List<RoleDTO>>(
                    true,
                    "User has no roles",
                    new List<RoleDTO>()
                );
            }

            // Obtener permisos por separado para evitar problemas de GROUP BY
            var roleIds = rolesData.Select(r => r.Id).ToList();
            var permissionsData = await (
                from rp in _db.RolePermissions
                join p in _db.Permissions on rp.PermissionId equals p.Id
                where roleIds.Contains(rp.RoleId)
                select new { rp.RoleId, p.Code }
            ).ToListAsync(ct);

            var permissionsByRole = permissionsData
                .GroupBy(x => x.RoleId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Code).ToList());

            // Construir el resultado
            var result = rolesData
                .Select(role => new RoleDTO
                {
                    Id = role.Id,
                    Name = role.Name,
                    Description = role.Description,
                    PortalAccess = role.PortalAccess,
                    PermissionCodes = permissionsByRole.TryGetValue(role.Id, out var permissions)
                        ? permissions
                        : new List<string>(),
                })
                .ToList();

            _log.LogInformation(
                "Retrieved {Count} roles for user {UserId}",
                result.Count,
                req.UserId
            );
            return new ApiResponse<List<RoleDTO>>(true, "Roles retrieved successfully", result);
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error retrieving roles for user {UserId}: {Message}",
                req.UserId,
                ex.Message
            );
            return new ApiResponse<List<RoleDTO>>(false, ex.Message, new List<RoleDTO>());
        }
    }
}
