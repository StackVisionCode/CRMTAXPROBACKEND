using Commands.RoleCommands;
using Common;
using Infraestructure.Context;
using MediatR;

namespace Handlers.RoleHandlers;

public class DeleteRoleHandler : IRequestHandler<DeleteRoleCommands, ApiResponse<bool>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly ILogger<DeleteRoleHandler> _logger;
  public DeleteRoleHandler(ApplicationDbContext dbContext, ILogger<DeleteRoleHandler> logger)
  {
    _dbContext = dbContext;
    _logger = logger;
  }
  public Task<ApiResponse<bool>> Handle(DeleteRoleCommands request, CancellationToken cancellationToken)
  {
    try
    {
      var role = _dbContext.Roles.FirstOrDefault(x => x.Id == request.RoleId);
      if (role == null)
      {
        return Task.FromResult(new ApiResponse<bool>(false, "Role not found", false));
      }
      _dbContext.Roles.Remove(role);
      var result = _dbContext.SaveChanges() > 0 ? true : false;
      _logger.LogInformation("Role deleted successfully: {RoleId}", request.RoleId);
      return Task.FromResult(new ApiResponse<bool>(result, result ? "Role deleted successfully" : "Failed to delete role", result));
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error deleting role: {Role}", request.RoleId);
      return Task.FromResult(new ApiResponse<bool>(false, "An error occurred while deleting the role", false));
    }
  }  
}