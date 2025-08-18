using AuthService.Domains.Permissions;
using AuthService.DTOs.CompanyPermissionDTOs;
using Commands.CompanyPermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CompanyPermissionHandlers;

/// Handler para asignar múltiples permisos a un UserCompany
public class BulkAssignCompanyPermissionsHandler
    : IRequestHandler<
        BulkAssignCompanyPermissionsCommand,
        ApiResponse<IEnumerable<CompanyPermissionDTO>>
    >
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<BulkAssignCompanyPermissionsHandler> _logger;

    public BulkAssignCompanyPermissionsHandler(
        ApplicationDbContext dbContext,
        ILogger<BulkAssignCompanyPermissionsHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<IEnumerable<CompanyPermissionDTO>>> Handle(
        BulkAssignCompanyPermissionsCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Verificar que el TaxUser existe
            var taxUser = await _dbContext.TaxUsers.FirstOrDefaultAsync(
                tu => tu.Id == request.TaxUserId && tu.IsActive,
                cancellationToken
            );

            if (taxUser == null)
            {
                _logger.LogWarning("TaxUser not found or inactive: {TaxUserId}", request.TaxUserId);
                return new ApiResponse<IEnumerable<CompanyPermissionDTO>>(
                    false,
                    "TaxUser not found or inactive",
                    null!
                );
            }

            // 2. Verificar que todos los permisos existen
            var permissionIds = request.Permissions.Select(p => p.PermissionId).ToList();
            var validPermissions = await _dbContext
                .Permissions.Where(p => permissionIds.Contains(p.Id) && p.IsGranted)
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            if (validPermissions.Count != permissionIds.Count)
            {
                return new ApiResponse<IEnumerable<CompanyPermissionDTO>>(
                    false,
                    "One or more permissions not found or inactive",
                    null!
                );
            }

            // 3. Verificar permisos ya existentes
            var existingPermissions = await _dbContext
                .CompanyPermissions.Where(cp =>
                    cp.TaxUserId == request.TaxUserId && permissionIds.Contains(cp.PermissionId)
                )
                .Select(cp => cp.PermissionId)
                .ToListAsync(cancellationToken);

            var newPermissions = request
                .Permissions.Where(p => !existingPermissions.Contains(p.PermissionId))
                .ToList();

            if (!newPermissions.Any())
            {
                return new ApiResponse<IEnumerable<CompanyPermissionDTO>>(
                    false,
                    "All permissions are already assigned to this user",
                    null!
                );
            }

            // 4. Crear CompanyPermissions
            var companyPermissions = newPermissions
                .Select(permDto => new CompanyPermission
                {
                    Id = Guid.NewGuid(),
                    TaxUserId = request.TaxUserId,
                    PermissionId = permDto.PermissionId,
                    IsGranted = permDto.IsGranted,
                    Description = permDto.Description?.Trim(),
                    CreatedAt = DateTime.UtcNow,
                })
                .ToList();

            await _dbContext.CompanyPermissions.AddRangeAsync(
                companyPermissions,
                cancellationToken
            );

            // 5. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<IEnumerable<CompanyPermissionDTO>>(
                    false,
                    "Failed to assign CompanyPermissions",
                    null!
                );
            }

            await transaction.CommitAsync(cancellationToken);

            // 6. Obtener CompanyPermissions creados para respuesta
            var createdPermissionIds = companyPermissions.Select(cp => cp.Id).ToList();
            var createdPermissionsQuery = await (
                from cp in _dbContext.CompanyPermissions
                join tu in _dbContext.TaxUsers on cp.TaxUserId equals tu.Id
                join p in _dbContext.Permissions on cp.PermissionId equals p.Id
                where createdPermissionIds.Contains(cp.Id)
                select new CompanyPermissionDTO
                {
                    Id = cp.Id,
                    TaxUserId = cp.TaxUserId,
                    PermissionId = cp.PermissionId,
                    IsGranted = cp.IsGranted,
                    Description = cp.Description,
                    UserEmail = tu.Email,
                    UserName = tu.Name,
                    UserLastName = tu.LastName,
                    PermissionCode = p.Code,
                    PermissionName = p.Name,
                    CreatedAt = cp.CreatedAt,
                }
            ).ToListAsync(cancellationToken);

            _logger.LogInformation(
                "Bulk assigned {Count} CompanyPermissions to TaxUser: {TaxUserId}",
                companyPermissions.Count,
                request.TaxUserId
            );

            return new ApiResponse<IEnumerable<CompanyPermissionDTO>>(
                true,
                "CompanyPermissions assigned successfully",
                createdPermissionsQuery
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error bulk assigning CompanyPermissions");
            return new ApiResponse<IEnumerable<CompanyPermissionDTO>>(
                false,
                "Error assigning CompanyPermissions",
                null!
            );
        }
    }
}
