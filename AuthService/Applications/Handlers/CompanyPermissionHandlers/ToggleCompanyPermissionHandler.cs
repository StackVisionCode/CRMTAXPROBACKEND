using AuthService.DTOs.CompanyPermissionDTOs;
using Commands.CompanyPermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CompanyPermissionHandlers;

/// Handler para toggle CompanyPermission
public class ToggleCompanyPermissionHandler
    : IRequestHandler<ToggleCompanyPermissionCommand, ApiResponse<CompanyPermissionDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ToggleCompanyPermissionHandler> _logger;

    public ToggleCompanyPermissionHandler(
        ApplicationDbContext dbContext,
        ILogger<ToggleCompanyPermissionHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyPermissionDTO>> Handle(
        ToggleCompanyPermissionCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Verificar que el CompanyPermission existe
            var companyPermission = await _dbContext.CompanyPermissions.FirstOrDefaultAsync(
                cp => cp.Id == request.CompanyPermissionId,
                cancellationToken
            );

            if (companyPermission == null)
            {
                _logger.LogWarning(
                    "CompanyPermission not found: {CompanyPermissionId}",
                    request.CompanyPermissionId
                );
                return new ApiResponse<CompanyPermissionDTO>(
                    false,
                    "CompanyPermission not found",
                    null!
                );
            }

            // 2. Actualizar estado
            companyPermission.IsGranted = request.IsGranted;
            companyPermission.UpdatedAt = DateTime.UtcNow;

            // 3. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<CompanyPermissionDTO>(
                    false,
                    "Failed to update CompanyPermission status",
                    null!
                );
            }

            await transaction.CommitAsync(cancellationToken);

            // 4. Obtener CompanyPermission actualizado para respuesta
            var updatedPermissionQuery = await (
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

            var action = request.IsGranted ? "granted" : "revoked";
            _logger.LogInformation(
                "CompanyPermission {Action}: {CompanyPermissionId}",
                action,
                companyPermission.Id
            );

            return new ApiResponse<CompanyPermissionDTO>(
                true,
                $"CompanyPermission {action} successfully",
                updatedPermissionQuery!
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error toggling CompanyPermission: {CompanyPermissionId}",
                request.CompanyPermissionId
            );
            return new ApiResponse<CompanyPermissionDTO>(
                false,
                "Error updating CompanyPermission status",
                null!
            );
        }
    }
}
