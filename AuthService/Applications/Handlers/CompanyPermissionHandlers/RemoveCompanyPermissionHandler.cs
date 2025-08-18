using Commands.CompanyPermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CompanyPermissionHandlers;

/// <summary>
/// Handler para remover CompanyPermission
/// </summary>
public class RemoveCompanyPermissionHandler
    : IRequestHandler<RemoveCompanyPermissionCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RemoveCompanyPermissionHandler> _logger;

    public RemoveCompanyPermissionHandler(
        ApplicationDbContext dbContext,
        ILogger<RemoveCompanyPermissionHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        RemoveCompanyPermissionCommand request,
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
                return new ApiResponse<bool>(false, "CompanyPermission not found", false);
            }

            // 2. Eliminar CompanyPermission
            _dbContext.CompanyPermissions.Remove(companyPermission);

            // 3. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<bool>(false, "Failed to remove CompanyPermission", false);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "CompanyPermission removed successfully: {CompanyPermissionId}",
                request.CompanyPermissionId
            );

            return new ApiResponse<bool>(true, "CompanyPermission removed successfully", true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error removing CompanyPermission: {CompanyPermissionId}",
                request.CompanyPermissionId
            );
            return new ApiResponse<bool>(false, "Error removing CompanyPermission", false);
        }
    }
}
