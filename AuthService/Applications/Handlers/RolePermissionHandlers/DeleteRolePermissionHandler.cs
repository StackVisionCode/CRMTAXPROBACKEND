using Commands.RolePermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.RolePermissionHandlers;

public class DeleteRolePermissionHandler : IRequestHandler<DeleteRolePermissionCommands, ApiResponse<bool>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly ILogger<DeleteRolePermissionHandler> _logger;
  public DeleteRolePermissionHandler(ApplicationDbContext dbContext, ILogger<DeleteRolePermissionHandler> logger)
  {
    _dbContext = dbContext;
    _logger = logger;
  }
  public async Task<ApiResponse<bool>> Handle(DeleteRolePermissionCommands request, CancellationToken cancellationToken)
  {
    try
    {
      var rolePermission = await _dbContext.RolePermissions.FirstOrDefaultAsync(x => x.Id == request.RolePermissionId, cancellationToken);
      if (rolePermission == null)
      {
        return new ApiResponse<bool>(false, "Role permission not found", false);
      }
      _dbContext.RolePermissions.Remove(rolePermission);
      var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0 ? true : false;
      _logger.LogInformation("Role permission deleted successfully: {RolePermissionId}", request.RolePermissionId);
      return new ApiResponse<bool>(result, result ? "Role permission deleted successfully" : "Failed to delete role permission", result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error deleting role permission: {RolePermissionId}", request.RolePermissionId);
      return new ApiResponse<bool>(false, "An error occurred while deleting the role permission", false);
    }
  }
}