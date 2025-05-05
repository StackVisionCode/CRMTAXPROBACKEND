using AuthService.DTOs.RoleDTOs;
using AutoMapper;
using Commands.RolePermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.RolePermissionHandlers;

public class GetAllRolePermissionHandler : IRequestHandler<GetAllRolePermissionQuery, ApiResponse<List<RolePermissionDTO>>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<GetAllRolePermissionHandler> _logger;
  public GetAllRolePermissionHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetAllRolePermissionHandler> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<List<RolePermissionDTO>>> Handle(GetAllRolePermissionQuery request, CancellationToken cancellationToken)
  {
    try
    {
      var rolePermissions = await _dbContext.RolePermissions.ToListAsync(cancellationToken);
      if (rolePermissions == null || !rolePermissions.Any())
      {
        return new ApiResponse<List<RolePermissionDTO>>(false, "No role permissions found", null!);
      }

      var rolePermissionDtos = _mapper.Map<List<RolePermissionDTO>>(rolePermissions);
      _logger.LogInformation("Role permissions retrieved successfully: {RolePermissions}", rolePermissionDtos);
      return new ApiResponse<List<RolePermissionDTO>>(true, "Role permissions retrieved successfully", rolePermissionDtos);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving role permissions: {Message}", ex.Message);
      return new ApiResponse<List<RolePermissionDTO>>(false, ex.Message, null!);
    }
  }
}