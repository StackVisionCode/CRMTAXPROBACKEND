using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyPermissionQueries;

namespace AuthService.Handlers.CompanyPermissionHandlers;

/// Handler para verificar si un TaxUser tiene un permiso específico
public class CheckTaxUserPermissionHandler
    : IRequestHandler<CheckTaxUserPermissionQuery, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<CheckTaxUserPermissionHandler> _logger;

    public CheckTaxUserPermissionHandler(
        ApplicationDbContext dbContext,
        ILogger<CheckTaxUserPermissionHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        CheckTaxUserPermissionQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Verificar que el TaxUser existe
            var taxUserExists = await _dbContext.TaxUsers.AnyAsync(
                tu => tu.Id == request.TaxUserId && tu.IsActive,
                cancellationToken
            );

            if (!taxUserExists)
            {
                return new ApiResponse<bool>(false, "TaxUser not found or inactive", false);
            }

            // Obtener permisos de roles
            var hasRolePermission = await (
                from ur in _dbContext.UserRoles
                join r in _dbContext.Roles on ur.RoleId equals r.Id
                join rp in _dbContext.RolePermissions on r.Id equals rp.RoleId
                join p in _dbContext.Permissions on rp.PermissionId equals p.Id
                where ur.TaxUserId == request.TaxUserId && p.Code == request.PermissionCode
                select p.Code
            ).AnyAsync(cancellationToken);

            // Obtener permisos personalizados para este código específico
            var customPermissionStatus = await (
                from cp in _dbContext.CompanyPermissions
                join p in _dbContext.Permissions on cp.PermissionId equals p.Id
                where cp.TaxUserId == request.TaxUserId && p.Code == request.PermissionCode
                select (bool?)cp.IsGranted
            ).FirstOrDefaultAsync(cancellationToken);

            // Lógica de permisos efectivos:
            // 1. Si hay permiso personalizado, usar ese estado (granted/revoked)
            // 2. Si no hay permiso personalizado, usar el permiso del rol
            bool hasPermission;
            if (customPermissionStatus.HasValue) // Existe permiso personalizado
            {
                hasPermission = customPermissionStatus.Value; // true = granted, false = revoked
            }
            else
            {
                hasPermission = hasRolePermission; // Usar permiso del rol
            }

            return new ApiResponse<bool>(true, "Permission check completed", hasPermission);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking permission: {TaxUserId}, {PermissionCode}",
                request.TaxUserId,
                request.PermissionCode
            );
            return new ApiResponse<bool>(false, "Error checking permission", false);
        }
    }
}
