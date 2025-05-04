using AuthService.Domains.Roles;
using AuthService.DTOs.RoleDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.RoleQueries;

namespace Handlers.RoleHandlers;

public class GetAllRoleHandler : IRequestHandler<GetAllRoleQuery, ApiResponse<List<RoleDTO>>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<GetAllRoleHandler> _logger;
  public GetAllRoleHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetAllRoleHandler> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<List<RoleDTO>>> Handle(GetAllRoleQuery request, CancellationToken cancellationToken)
  {
    try
    {
      var roles = await _dbContext.Roles.ToListAsync(cancellationToken);
      if (roles == null || !roles.Any())
      {
        return new ApiResponse<List<RoleDTO>>(false, "No roles found", null!);
      }

      var roleDtos = _mapper.Map<List<RoleDTO>>(roles);
      _logger.LogInformation("Roles retrieved successfully: {Roles}", roleDtos);
      return new ApiResponse<List<RoleDTO>>(true, "Roles retrieved successfully", roleDtos);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving roles: {Message}", ex.Message);
      return new ApiResponse<List<RoleDTO>>(false, ex.Message, null!);
    }
  }
}