using AuthService.DTOs.RoleDTOs;
using AutoMapper;
using Commands.RolePermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.RolePermissionHandlers;

public class GetRolePermissionByIdHandler : IRequestHandler<GetRolePermissionByIdQuery, ApiResponse<RolePermissionDTO>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<GetRolePermissionByIdHandler> _logger;
  public GetRolePermissionByIdHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetRolePermissionByIdHandler> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<RolePermissionDTO>> Handle(GetRolePermissionByIdQuery request, CancellationToken cancellationToken)
  {
    try
    {
      var rolePermission = await _dbContext.RolePermissions
                                  .AsNoTracking()
                                  .FirstOrDefaultAsync(u => u.Id == request.RolePermissionId, cancellationToken);

      if (rolePermission is null)
        return new(false, "Role permission not found");

      var rolePermissionDto = _mapper.Map<RolePermissionDTO>(rolePermission);

      return new ApiResponse<RolePermissionDTO>(true, "Ok", rolePermissionDto);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error fetching role permission {Id}", request.RolePermissionId);
      return new(false, ex.Message);
    }
  }
}