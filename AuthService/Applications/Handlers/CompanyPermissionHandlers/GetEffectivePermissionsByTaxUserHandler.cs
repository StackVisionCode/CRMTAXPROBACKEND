using AuthService.DTOs.CompanyPermissionDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyPermissionQueries;

namespace AuthService.Handlers.CompanyPermissionHandlers;

/// Handler para obtener permisos efectivos de un UserCompany
public class GetEffectivePermissionsByTaxUserHandler
    : IRequestHandler<GetEffectivePermissionsByTaxUserQuery, ApiResponse<CompanyUserPermissionsDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetEffectivePermissionsByTaxUserHandler> _logger;

    public GetEffectivePermissionsByTaxUserHandler(
        ApplicationDbContext dbContext,
        ILogger<GetEffectivePermissionsByTaxUserHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyUserPermissionsDTO>> Handle(
        GetEffectivePermissionsByTaxUserQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Verificar que el TaxUser existe
            var taxUser = await _dbContext.TaxUsers.FirstOrDefaultAsync(
                tu => tu.Id == request.TaxUserId,
                cancellationToken
            );

            if (taxUser == null)
            {
                return new ApiResponse<CompanyUserPermissionsDTO>(
                    false,
                    "TaxUser not found",
                    null!
                );
            }

            // Obtener permisos personalizados (todos: granted y revoked)
            var customPermissionsQuery = await (
                from cp in _dbContext.CompanyPermissions
                join p in _dbContext.Permissions on cp.PermissionId equals p.Id
                where cp.TaxUserId == request.TaxUserId
                select new CompanyPermissionDTO
                {
                    Id = cp.Id,
                    TaxUserId = cp.TaxUserId,
                    PermissionId = cp.PermissionId,
                    IsGranted = cp.IsGranted,
                    Description = cp.Description,
                    UserEmail = taxUser.Email,
                    UserName = taxUser.Name,
                    UserLastName = taxUser.LastName,
                    PermissionCode = p.Code,
                    PermissionName = p.Name,
                    CreatedAt = cp.CreatedAt,
                }
            ).ToListAsync(cancellationToken);

            // Obtener permisos de roles
            var rolePermissionsQuery = await (
                from ur in _dbContext.UserRoles
                join r in _dbContext.Roles on ur.RoleId equals r.Id
                join rp in _dbContext.RolePermissions on r.Id equals rp.RoleId
                join p in _dbContext.Permissions on rp.PermissionId equals p.Id
                where ur.TaxUserId == request.TaxUserId
                select p.Code
            )
                .Distinct()
                .ToListAsync(cancellationToken);

            // Calcular permisos efectivos
            var grantedCustomPermissions = customPermissionsQuery
                .Where(cp => cp.IsGranted)
                .Select(cp => cp.PermissionCode!);
            var revokedCustomPermissions = customPermissionsQuery
                .Where(cp => !cp.IsGranted)
                .Select(cp => cp.PermissionCode!);

            var effectivePermissions = rolePermissionsQuery
                .Concat(grantedCustomPermissions)
                .Except(revokedCustomPermissions)
                .Distinct()
                .ToList();

            var userPermissionsDto = new CompanyUserPermissionsDTO
            {
                TaxUserId = request.TaxUserId,
                UserEmail = taxUser.Email,
                UserName = taxUser.Name,
                UserLastName = taxUser.LastName,
                IsOwner = taxUser.IsOwner,
                CustomPermissions = customPermissionsQuery,
                RoleBasedPermissions = rolePermissionsQuery,
                EffectivePermissions = effectivePermissions,
            };

            return new ApiResponse<CompanyUserPermissionsDTO>(
                true,
                "Effective permissions retrieved successfully",
                userPermissionsDto
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting effective permissions: {TaxUserId}",
                request.TaxUserId
            );
            return new ApiResponse<CompanyUserPermissionsDTO>(
                false,
                "Error retrieving effective permissions",
                null!
            );
        }
    }
}
