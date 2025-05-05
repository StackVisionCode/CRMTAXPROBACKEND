using AutoMapper;
using Commands.PermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.PermissionHandlers;

public class DeletePermissionHandler : IRequestHandler<DeletePermissionCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeletePermissionHandler> _logger;

    public DeletePermissionHandler(ApplicationDbContext dbContext,ILogger<DeletePermissionHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(DeletePermissionCommands request, CancellationToken cancellationToken)
    {
        try
        {
            var permission = await _dbContext.Permissions.FirstOrDefaultAsync(x => x.Id == request.PermissionId, cancellationToken);
            if (permission == null)
            {
                return new ApiResponse<bool>(false, "Permission not found", false);
            }

            _dbContext.Permissions.Remove(permission);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0 ? true : false;
            _logger.LogInformation("Permission deleted successfully: {Permission}", permission);
            return new ApiResponse<bool>(result, result ? "Permission deleted successfully" : "Failed to delete permission", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting permission: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}