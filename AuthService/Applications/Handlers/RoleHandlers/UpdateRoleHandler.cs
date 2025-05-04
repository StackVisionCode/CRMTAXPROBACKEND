using AutoMapper;
using Commands.RoleCommands;
using Common;
using Infraestructure.Context;
using MediatR;

namespace Handlers.RoleHandlers;

public class UpdateRoleHandler : IRequestHandler<UpdateRoleCommands, ApiResponse<bool>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<UpdateRoleHandler> _logger;
  public UpdateRoleHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<UpdateRoleHandler> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<bool>> Handle(UpdateRoleCommands request, CancellationToken cancellationToken)
  {
    try
    {
      var role = await _dbContext.Roles.FindAsync(new object[] { request.Role.Id }, cancellationToken);
      if (role == null)
      {
        return new ApiResponse<bool>(false, "Role not found", false);
      }

      _mapper.Map(request.Role, role);
      role.UpdatedAt = DateTime.UtcNow;
      _dbContext.Roles.Update(role);
      var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0 ? true : false;
      _logger.LogInformation("Role updated successfully: {Role}", role);
      return new ApiResponse<bool>(result, result ? "Role updated successfully" : "Failed to update role", result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error updating role: {Message}", ex.Message);
      return new ApiResponse<bool>(false, ex.Message, false);
    }
  }
}