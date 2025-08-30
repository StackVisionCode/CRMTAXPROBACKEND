using AuthService.DTOs.PermissionDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyPermissionQueries;

namespace AuthService.Handlers.CompanyPermissionHandlers;

/// Handler para obtener permisos disponibles para asignar a un TaxUser
public class GetAvailablePermissionsForTaxUserHandler
    : IRequestHandler<
        GetAvailablePermissionsForTaxUserQuery,
        ApiResponse<IEnumerable<PermissionDTO>>
    >
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetAvailablePermissionsForTaxUserHandler> _logger;

    public GetAvailablePermissionsForTaxUserHandler(
        ApplicationDbContext dbContext,
        ILogger<GetAvailablePermissionsForTaxUserHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<PermissionDTO>>> Handle(
        GetAvailablePermissionsForTaxUserQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // 1. Verificar que el TaxUser existe y obtener su CompanyId
            var taxUser = await _dbContext.TaxUsers.FirstOrDefaultAsync(
                tu => tu.Id == request.TaxUserId && tu.IsActive,
                cancellationToken
            );

            if (taxUser == null)
            {
                return new ApiResponse<IEnumerable<PermissionDTO>>(
                    false,
                    "TaxUser not found or inactive",
                    null!
                );
            }

            // 2. Encontrar el Owner de esta Company
            var ownerTaxUser = await _dbContext.TaxUsers.FirstOrDefaultAsync(
                tu => tu.CompanyId == taxUser.CompanyId && tu.IsOwner && tu.IsActive,
                cancellationToken
            );

            if (ownerTaxUser == null)
            {
                return new ApiResponse<IEnumerable<PermissionDTO>>(
                    false,
                    "Company owner not found",
                    null!
                );
            }

            // 3. Obtener permisos que tiene el Owner por sus roles (estos son los "delegables")
            var ownerPermissionIds = await (
                from ur in _dbContext.UserRoles
                join rp in _dbContext.RolePermissions on ur.RoleId equals rp.RoleId
                where ur.TaxUserId == ownerTaxUser.Id
                select rp.PermissionId
            )
                .Distinct()
                .ToListAsync(cancellationToken);

            // 4. Obtener permisos ya asignados como CompanyPermissions a este usuario específico
            var assignedPermissionIds = await (
                from cp in _dbContext.CompanyPermissions
                where cp.TaxUserId == request.TaxUserId
                select cp.PermissionId
            ).ToListAsync(cancellationToken);

            // 5. Los permisos disponibles son los del Owner que no están asignados al usuario
            var availablePermissions = await (
                from p in _dbContext.Permissions
                where
                    p.IsGranted
                    && ownerPermissionIds.Contains(p.Id) // Permisos del Owner
                    && !assignedPermissionIds.Contains(p.Id) // No asignados al usuario
                orderby p.Name
                select new PermissionDTO
                {
                    Id = p.Id,
                    Name = p.Name,
                    Code = p.Code,
                    Description = p.Description,
                    IsGranted = p.IsGranted,
                }
            ).ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Available permissions for TaxUser {TaxUserId} in Company {CompanyId}: Owner permissions={OwnerPermissions}, Already assigned={AssignedCount}, Available={AvailableCount}",
                request.TaxUserId,
                taxUser.CompanyId,
                ownerPermissionIds.Count,
                assignedPermissionIds.Count,
                availablePermissions.Count
            );

            return new ApiResponse<IEnumerable<PermissionDTO>>(
                true,
                "Available permissions retrieved successfully",
                availablePermissions
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting available permissions for TaxUser: {TaxUserId}",
                request.TaxUserId
            );
            return new ApiResponse<IEnumerable<PermissionDTO>>(
                false,
                "Error retrieving available permissions",
                null!
            );
        }
    }
}
