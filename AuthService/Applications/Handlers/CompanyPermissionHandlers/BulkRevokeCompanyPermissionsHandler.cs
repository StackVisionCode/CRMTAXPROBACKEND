using Commands.CompanyPermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CompanyPermissionHandlers;

/// Handler para revocar múltiples permisos de un UserCompany
public class BulkRevokeCompanyPermissionsHandler
    : IRequestHandler<BulkRevokeCompanyPermissionsCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BulkRevokeCompanyPermissionsHandler> _logger;

    public BulkRevokeCompanyPermissionsHandler(
        ApplicationDbContext dbContext,
        ILogger<BulkRevokeCompanyPermissionsHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        BulkRevokeCompanyPermissionsCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Verificar que el TaxUser existe
            var taxUserExists = await _dbContext.TaxUsers.AnyAsync(
                tu => tu.Id == request.TaxUserId && tu.IsActive,
                cancellationToken
            );

            if (!taxUserExists)
            {
                _logger.LogWarning("TaxUser not found or inactive: {TaxUserId}", request.TaxUserId);
                return new ApiResponse<bool>(false, "TaxUser not found or inactive", false);
            }

            // 2. Obtener CompanyPermissions a revocar usando join con Permissions
            var companyPermissionsToRevoke = await (
                from cp in _dbContext.CompanyPermissions
                join p in _dbContext.Permissions on cp.PermissionId equals p.Id
                where cp.TaxUserId == request.TaxUserId && request.PermissionCodes.Contains(p.Code)
                select cp
            ).ToListAsync(cancellationToken);

            if (!companyPermissionsToRevoke.Any())
            {
                return new ApiResponse<bool>(false, "No CompanyPermissions found to revoke", false);
            }

            // 3. Eliminar CompanyPermissions
            _dbContext.CompanyPermissions.RemoveRange(companyPermissionsToRevoke);

            // 4. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<bool>(false, "Failed to revoke CompanyPermissions", false);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Bulk revoked {Count} CompanyPermissions from TaxUser: {TaxUserId}",
                companyPermissionsToRevoke.Count,
                request.TaxUserId
            );

            return new ApiResponse<bool>(
                true,
                $"Revoked {companyPermissionsToRevoke.Count} CompanyPermissions successfully",
                true
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error bulk revoking CompanyPermissions");
            return new ApiResponse<bool>(false, "Error revoking CompanyPermissions", false);
        }
    }
}
