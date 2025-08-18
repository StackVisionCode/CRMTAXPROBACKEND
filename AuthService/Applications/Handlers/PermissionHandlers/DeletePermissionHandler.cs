using Commands.PermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.PermissionHandlers;

public class DeletePermissionHandler : IRequestHandler<DeletePermissionCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeletePermissionHandler> _logger;

    public DeletePermissionHandler(
        ApplicationDbContext dbContext,
        ILogger<DeletePermissionHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeletePermissionCommands request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Buscar el permiso con información de uso correcta
            var permissionInfoQuery =
                from p in _dbContext.Permissions
                where p.Id == request.PermissionId
                select new
                {
                    Permission = p,
                    UsedInRoles = _dbContext.RolePermissions.Count(rp => rp.PermissionId == p.Id),
                    // CompanyPermissions usa PermissionId, no Code
                    UsedInCompanyPermissions = _dbContext.CompanyPermissions.Count(cp =>
                        cp.PermissionId == p.Id
                    ),
                };

            var permissionInfo = await permissionInfoQuery.FirstOrDefaultAsync(cancellationToken);
            if (permissionInfo?.Permission == null)
            {
                _logger.LogWarning("Permission not found: {PermissionId}", request.PermissionId);
                return new ApiResponse<bool>(false, "Permission not found", false);
            }

            var permission = permissionInfo.Permission;

            // No permitir eliminar permisos del sistema
            var systemPermissionPrefixes = new[]
            {
                "Permission.",
                "TaxUser.",
                "Customer.",
                "Role.",
                "RolePermission.",
                "Service.",
                "Module.",
                "CustomPlan.",
                "CustomModule.",
                "Company.",
            };

            var isSystemPermission = systemPermissionPrefixes.Any(prefix =>
                permission.Code.StartsWith(prefix)
            );

            if (isSystemPermission)
            {
                _logger.LogWarning("Cannot delete system permission: {Code}", permission.Code);
                return new ApiResponse<bool>(
                    false,
                    "Cannot delete system permissions. These are required for core functionality.",
                    false
                );
            }

            // Log de información de uso
            _logger.LogInformation(
                "Deleting permission {Code}: Used in {RoleCount} roles and {CompanyPermCount} company permissions",
                permission.Code,
                permissionInfo.UsedInRoles,
                permissionInfo.UsedInCompanyPermissions
            );

            // Eliminar RolePermissions primero
            if (permissionInfo.UsedInRoles > 0)
            {
                var rolePermissionsQuery =
                    from rp in _dbContext.RolePermissions
                    where rp.PermissionId == permission.Id
                    select rp;

                var rolePermissions = await rolePermissionsQuery.ToListAsync(cancellationToken);
                _dbContext.RolePermissions.RemoveRange(rolePermissions);
                _logger.LogDebug(
                    "Marked {Count} role permissions for deletion",
                    rolePermissions.Count
                );
            }

            // Eliminar CompanyPermissions relacionadas
            if (permissionInfo.UsedInCompanyPermissions > 0)
            {
                var companyPermissionsQuery =
                    from cp in _dbContext.CompanyPermissions
                    where cp.PermissionId == permission.Id
                    select cp;

                var companyPermissions = await companyPermissionsQuery.ToListAsync(
                    cancellationToken
                );
                _dbContext.CompanyPermissions.RemoveRange(companyPermissions);
                _logger.LogDebug(
                    "Marked {Count} company permissions for deletion",
                    companyPermissions.Count
                );
            }

            // Eliminar el permiso
            _dbContext.Permissions.Remove(permission);

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Permission deleted successfully: {Code} (removed from {RoleCount} roles and {CompanyPermCount} company permissions)",
                    permission.Code,
                    permissionInfo.UsedInRoles,
                    permissionInfo.UsedInCompanyPermissions
                );
                return new ApiResponse<bool>(true, "Permission deleted successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError(
                    "Failed to delete permission: {PermissionId}",
                    request.PermissionId
                );
                return new ApiResponse<bool>(false, "Failed to delete permission", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error deleting permission {PermissionId}: {Message}",
                request.PermissionId,
                ex.Message
            );
            return new ApiResponse<bool>(
                false,
                "An error occurred while deleting the permission",
                false
            );
        }
    }
}
