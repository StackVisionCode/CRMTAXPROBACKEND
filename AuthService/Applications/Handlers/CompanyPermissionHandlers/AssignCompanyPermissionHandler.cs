using AuthService.Domains.Permissions;
using AuthService.DTOs.CompanyPermissionDTOs;
using Commands.CompanyPermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CompanyPermissionHandlers;

/// <summary>
/// Handler para asignar CompanyPermission
/// </summary>
public class AssignCompanyPermissionHandler
    : IRequestHandler<AssignCompanyPermissionCommand, ApiResponse<CompanyPermissionDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AssignCompanyPermissionHandler> _logger;

    public AssignCompanyPermissionHandler(
        ApplicationDbContext dbContext,
        ILogger<AssignCompanyPermissionHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyPermissionDTO>> Handle(
        AssignCompanyPermissionCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dto = request.CompanyPermissionData;

            // 1. Verificar que el TaxUser existe y obtener su CompanyId
            var taxUserQuery = await (
                from tu in _dbContext.TaxUsers
                where tu.Id == dto.TaxUserId && tu.IsActive
                select new
                {
                    tu.Id,
                    tu.CompanyId,
                    tu.Email,
                    tu.Name,
                    tu.LastName,
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (taxUserQuery == null)
            {
                _logger.LogWarning("TaxUser not found or inactive: {TaxUserId}", dto.TaxUserId);
                return new ApiResponse<CompanyPermissionDTO>(
                    false,
                    "TaxUser not found or inactive",
                    null!
                );
            }

            // 2. Verificar que el Permission existe
            var permission = await _dbContext.Permissions.FirstOrDefaultAsync(
                p => p.Id == dto.PermissionId && p.IsGranted,
                cancellationToken
            );

            if (permission == null)
            {
                _logger.LogWarning("Permission not found: {PermissionId}", dto.PermissionId);
                return new ApiResponse<CompanyPermissionDTO>(false, "Permission not found", null!);
            }

            // 3. Verificar que no existe ya este permiso para este TaxUser
            var existingPermission = await _dbContext.CompanyPermissions.FirstOrDefaultAsync(
                cp => cp.TaxUserId == dto.TaxUserId && cp.PermissionId == dto.PermissionId,
                cancellationToken
            );

            if (existingPermission != null)
            {
                _logger.LogWarning(
                    "CompanyPermission already exists: TaxUser {TaxUserId}, Permission {PermissionId}",
                    dto.TaxUserId,
                    dto.PermissionId
                );
                return new ApiResponse<CompanyPermissionDTO>(
                    false,
                    "Permission already assigned to this user",
                    null!
                );
            }

            // 4. Crear CompanyPermission
            var companyPermission = new CompanyPermission
            {
                Id = Guid.NewGuid(),
                TaxUserId = dto.TaxUserId,
                PermissionId = dto.PermissionId,
                IsGranted = dto.IsGranted,
                Description = dto.Description?.Trim(),
                CreatedAt = DateTime.UtcNow,
            };

            await _dbContext.CompanyPermissions.AddAsync(companyPermission, cancellationToken);

            // 5. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<CompanyPermissionDTO>(
                    false,
                    "Failed to assign CompanyPermission",
                    null!
                );
            }

            await transaction.CommitAsync(cancellationToken);

            // 6. Obtener CompanyPermission completo para respuesta con join
            var createdPermissionQuery = await (
                from cp in _dbContext.CompanyPermissions
                join tu in _dbContext.TaxUsers on cp.TaxUserId equals tu.Id
                join p in _dbContext.Permissions on cp.PermissionId equals p.Id
                where cp.Id == companyPermission.Id
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
            ).FirstOrDefaultAsync(cancellationToken);

            _logger.LogInformation(
                "CompanyPermission assigned successfully: {CompanyPermissionId}",
                companyPermission.Id
            );

            return new ApiResponse<CompanyPermissionDTO>(
                true,
                "CompanyPermission assigned successfully",
                createdPermissionQuery!
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error assigning CompanyPermission");
            return new ApiResponse<CompanyPermissionDTO>(
                false,
                "Error assigning CompanyPermission",
                null!
            );
        }
    }
}
