using Commands.CustomModuleCommands;
using Common;
using Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Handlers.CustomModuleHandlers;

/// Handler para remover CustomModule
public class RemoveCustomModuleHandler
    : IRequestHandler<RemoveCustomModuleCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RemoveCustomModuleHandler> _logger;

    public RemoveCustomModuleHandler(
        ApplicationDbContext dbContext,
        ILogger<RemoveCustomModuleHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        RemoveCustomModuleCommand request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Verificar que el CustomModule existe
            var customModule = await _dbContext.CustomModules.FirstOrDefaultAsync(
                cm => cm.Id == request.CustomModuleId,
                cancellationToken
            );

            if (customModule == null)
            {
                _logger.LogWarning(
                    "CustomModule not found: {CustomModuleId}",
                    request.CustomModuleId
                );
                return new ApiResponse<bool>(false, "CustomModule not found", false);
            }

            // 2. Eliminar CustomModule
            _dbContext.CustomModules.Remove(customModule);

            // 3. Guardar cambios
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (!result)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new ApiResponse<bool>(false, "Failed to remove CustomModule", false);
            }

            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "CustomModule removed successfully: {CustomModuleId}",
                request.CustomModuleId
            );

            return new ApiResponse<bool>(true, "CustomModule removed successfully", true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Error removing CustomModule: {CustomModuleId}",
                request.CustomModuleId
            );
            return new ApiResponse<bool>(false, "Error removing CustomModule", false);
        }
    }
}
