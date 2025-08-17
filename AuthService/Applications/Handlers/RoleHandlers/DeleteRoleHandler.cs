using Commands.RoleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.RoleHandlers;

public class DeleteRoleHandler : IRequestHandler<DeleteRoleCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteRoleHandler> _logger;

    public DeleteRoleHandler(ApplicationDbContext dbContext, ILogger<DeleteRoleHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteRoleCommands request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Solo verificar uso en TaxUsers (UserRoles)
            var usageQuery =
                from r in _dbContext.Roles
                where r.Id == request.RoleId
                select new
                {
                    Role = r,
                    UsedByTaxUsers = _dbContext.UserRoles.Any(ur => ur.RoleId == request.RoleId),
                    // Verificar si es un rol del sistema que no se puede eliminar
                    IsSystemRole = r.Name == "Developer"
                        || r.Name.Contains("Administrator")
                        || r.Name == "User"
                        || r.Name == "Customer",
                };

            var usage = await usageQuery.FirstOrDefaultAsync(cancellationToken);
            if (usage?.Role == null)
            {
                _logger.LogWarning("Role not found: {RoleId}", request.RoleId);
                return new ApiResponse<bool>(false, "Role not found", false);
            }

            // No permitir eliminar roles del sistema
            if (usage.IsSystemRole)
            {
                _logger.LogWarning("Cannot delete system role: {RoleName}", usage.Role.Name);
                return new ApiResponse<bool>(
                    false,
                    "Cannot delete system roles (Developer, Administrator, User, Customer)",
                    false
                );
            }

            // Verificar uso por TaxUsers
            if (usage.UsedByTaxUsers)
            {
                // Contar cuántos usuarios lo usan
                var userCountQuery =
                    from ur in _dbContext.UserRoles
                    where ur.RoleId == request.RoleId
                    select ur.TaxUserId;

                var userCount = await userCountQuery.CountAsync(cancellationToken);

                _logger.LogWarning(
                    "Role {RoleId} ({RoleName}) is in use by {UserCount} users",
                    request.RoleId,
                    usage.Role.Name,
                    userCount
                );
                return new ApiResponse<bool>(
                    false,
                    $"Role is in use by {userCount} user(s). Reassign users before deleting.",
                    false
                );
            }

            // Eliminar RolePermissions primero
            var rolePermissionsQuery =
                from rp in _dbContext.RolePermissions
                where rp.RoleId == request.RoleId
                select rp;

            var rolePermissions = await rolePermissionsQuery.ToListAsync(cancellationToken);
            if (rolePermissions.Any())
            {
                _dbContext.RolePermissions.RemoveRange(rolePermissions);
                _logger.LogDebug(
                    "Marked {Count} role permissions for deletion",
                    rolePermissions.Count
                );
            }

            // Eliminar el rol
            _dbContext.Roles.Remove(usage.Role);

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Role deleted successfully: {RoleName} (ServiceLevel: {ServiceLevel})",
                    usage.Role.Name,
                    usage.Role.ServiceLevel
                );
                return new ApiResponse<bool>(true, "Role deleted successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to delete role: {RoleId}", request.RoleId);
                return new ApiResponse<bool>(false, "Failed to delete role", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error deleting role {RoleId}: {Message}",
                request.RoleId,
                ex.Message
            );
            return new ApiResponse<bool>(false, "An error occurred while deleting the role", false);
        }
    }
}
