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
            // Verificar que el TaxUser existe
            var taxUserExists = await _dbContext.TaxUsers.AnyAsync(
                tu => tu.Id == request.TaxUserId && tu.IsActive,
                cancellationToken
            );

            if (!taxUserExists)
            {
                return new ApiResponse<IEnumerable<PermissionDTO>>(
                    false,
                    "TaxUser not found or inactive",
                    null!
                );
            }

            // Obtener solo permisos que están en uso en CompanyPermissions
            // Esto asegura que solo mostramos permisos gestionables a nivel company
            var manageablePermissionIds = await (
                from cp in _dbContext.CompanyPermissions
                select cp.PermissionId
            )
                .Distinct()
                .ToListAsync(cancellationToken);

            // Obtener permisos que ya tiene asignados este usuario específico
            var assignedPermissionIds = await (
                from cp in _dbContext.CompanyPermissions
                where cp.TaxUserId == request.TaxUserId
                select cp.PermissionId
            ).ToListAsync(cancellationToken);

            // Solo devolver permisos que:
            // 1. Son gestionables (están en uso en CompanyPermissions)
            // 2. Están activos
            // 3. NO están asignados a este usuario específico
            var availablePermissions = await (
                from p in _dbContext.Permissions
                where
                    p.IsGranted
                    && manageablePermissionIds.Contains(p.Id)
                    && !assignedPermissionIds.Contains(p.Id)
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
                "Available permissions for TaxUser {TaxUserId}: Total manageable={ManageableCount}, Already assigned={AssignedCount}, Available={AvailableCount}",
                request.TaxUserId,
                manageablePermissionIds.Count,
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
