using AuthService.DTOs.PermissionDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.PermissionQueries;

namespace Handlers.PermissionHandlers;

public class GetUserPermissionsHandler
    : IRequestHandler<GetUserPermissionsQuery, ApiResponse<UserPermissionsDTO>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GetUserPermissionsHandler> _log;

    public GetUserPermissionsHandler(
        ApplicationDbContext db,
        ILogger<GetUserPermissionsHandler> log
    )
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<UserPermissionsDTO>> Handle(
        GetUserPermissionsQuery req,
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
                return new ApiResponse<UserPermissionsDTO>(false, "User not found");
            }

            // Obtener permisos de roles
            var rolePermissionsQuery =
                from ur in _db.UserRoles
                where ur.TaxUserId == req.UserId
                join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
                join p in _db.Permissions on rp.PermissionId equals p.Id
                select p.Code;

            var rolePermissions = await rolePermissionsQuery.Distinct().ToListAsync(ct);

            // Obtener permisos personalizados de CompanyPermissions
            var customPermissionsQuery =
                from cp in _db.CompanyPermissions
                join p in _db.Permissions on cp.PermissionId equals p.Id
                where cp.TaxUserId == req.UserId && cp.IsGranted
                select p.Code;

            var customGrantedPermissions = await customPermissionsQuery.Distinct().ToListAsync(ct);

            // Obtener permisos revocados de CompanyPermissions
            var revokedPermissionsQuery =
                from cp in _db.CompanyPermissions
                join p in _db.Permissions on cp.PermissionId equals p.Id
                where cp.TaxUserId == req.UserId && !cp.IsGranted
                select p.Code;

            var revokedPermissions = await revokedPermissionsQuery.Distinct().ToListAsync(ct);

            // CALCULAR PERMISOS EFECTIVOS:
            // Permisos de roles + permisos personalizados granted - permisos revocados
            var effectivePermissions = rolePermissions
                .Concat(customGrantedPermissions)
                .Except(revokedPermissions)
                .Distinct()
                .ToList();

            if (!effectivePermissions.Any())
            {
                _log.LogInformation("User {UserId} has no effective permissions", req.UserId);
                return new ApiResponse<UserPermissionsDTO>(
                    true,
                    "User has no permissions",
                    new UserPermissionsDTO
                    {
                        UserId = req.UserId,
                        PermissionCodes = new List<string>(),
                    }
                );
            }

            var dto = new UserPermissionsDTO
            {
                UserId = req.UserId,
                PermissionCodes = effectivePermissions,
            };

            _log.LogInformation(
                "Retrieved permissions for user {UserId}: RolePermissions={RoleCount}, CustomGranted={GrantedCount}, Revoked={RevokedCount}, Effective={EffectiveCount}",
                req.UserId,
                rolePermissions.Count,
                customGrantedPermissions.Count,
                revokedPermissions.Count,
                effectivePermissions.Count
            );

            return new ApiResponse<UserPermissionsDTO>(
                true,
                "Permissions retrieved successfully",
                dto
            );
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error retrieving permissions for user {UserId}: {Message}",
                req.UserId,
                ex.Message
            );
            return new ApiResponse<UserPermissionsDTO>(false, ex.Message);
        }
    }
}
