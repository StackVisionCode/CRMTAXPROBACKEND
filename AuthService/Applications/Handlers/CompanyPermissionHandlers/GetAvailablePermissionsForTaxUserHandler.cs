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

            // Obtener permisos que ya tiene asignados (custom permissions)
            var assignedPermissionIds = await (
                from cp in _dbContext.CompanyPermissions
                where cp.TaxUserId == request.TaxUserId
                select cp.PermissionId
            ).ToListAsync(cancellationToken);

            // Obtener todos los permisos activos que no están asignados
            var availablePermissions = await (
                from p in _dbContext.Permissions
                where p.IsGranted && !assignedPermissionIds.Contains(p.Id)
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
