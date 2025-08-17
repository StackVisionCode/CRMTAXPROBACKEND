using AuthService.DTOs.CompanyPermissionDTOs;
using Commands.CompanyPermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CompanyPermissionHandlers;

/// Handler para actualizar CompanyPermission
public class UpdateCompanyPermissionHandler
    : IRequestHandler<UpdateCompanyPermissionCommand, ApiResponse<CompanyPermissionDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<UpdateCompanyPermissionHandler> _logger;

    public UpdateCompanyPermissionHandler(
        ApplicationDbContext dbContext,
        ILogger<UpdateCompanyPermissionHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<CompanyPermissionDTO>> Handle(
        UpdateCompanyPermissionCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var dto = request.CompanyPermissionData;

            // 1. Verificar que el CompanyPermission existe
            var companyPermission = await _dbContext.CompanyPermissions.FirstOrDefaultAsync(
                cp => cp.Id == dto.Id,
                cancellationToken
            );

            if (companyPermission == null)
            {
                _logger.LogWarning("CompanyPermission not found: {CompanyPermissionId}", dto.Id);
                return new ApiResponse<CompanyPermissionDTO>(
                    false,
                    "CompanyPermission not found",
                    null!
                );
            }

            // 2. Actualizar CompanyPermission
            companyPermission.IsGranted = dto.IsGranted;
            companyPermission.Description = dto.Description?.Trim();
            companyPermission.UpdatedAt = DateTime.UtcNow;

            // 3. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<CompanyPermissionDTO>(
                    false,
                    "Failed to update CompanyPermission",
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

            _logger.LogInformation(
                "CompanyPermission updated successfully: {CompanyPermissionId}",
                companyPermission.Id
            );

            return new ApiResponse<CompanyPermissionDTO>(
                true,
                "CompanyPermission updated successfully",
                updatedPermissionQuery!
            );
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error updating CompanyPermission: {CompanyPermissionId}",
                request.CompanyPermissionData.Id
            );
            return new ApiResponse<CompanyPermissionDTO>(
                false,
                "Error updating CompanyPermission",
                null!
            );
        }
    }
}
