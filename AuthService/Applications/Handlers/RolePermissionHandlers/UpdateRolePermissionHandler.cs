using AutoMapper;
using Commands.RolePermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;

namespace Handlers.RolePermissionHandlers;

public class UpdateRolePermissionHandler : IRequestHandler<UpdateRolePermissionCommands, ApiResponse<bool>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<UpdateRolePermissionHandler> _logger;
  public UpdateRolePermissionHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<UpdateRolePermissionHandler> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<bool>> Handle(UpdateRolePermissionCommands request, CancellationToken cancellationToken)
  {
    try
    {
      var rolePermission = await _dbContext.RolePermissions.FindAsync(new object[] { request.rolePermission.Id }, cancellationToken);
      if (rolePermission == null)
      {
        return new ApiResponse<bool>(false, "Role permission not found", false);
      }

      _mapper.Map(request.rolePermission, rolePermission);
      rolePermission.UpdatedAt = DateTime.UtcNow;
      _dbContext.RolePermissions.Update(rolePermission);
      var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0 ? true : false;
      _logger.LogInformation("Role permission updated successfully: {RolePermission}", rolePermission);
      return new ApiResponse<bool>(result, result ? "Role permission updated successfully" : "Failed to update role permission", result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating role permission: {Message}", ex.Message);
      return new ApiResponse<bool>(false, ex.Message, false);
    }
  }
}