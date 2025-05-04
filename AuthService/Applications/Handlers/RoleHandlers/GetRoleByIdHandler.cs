using AuthService.DTOs.RoleDTOs;
using AutoMapper;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.RoleQueries;

namespace Handlers.RoleHandlers;

public class GetRoleByIdHandler : IRequestHandler<GetRoleByIdQuery, ApiResponse<RoleDTO>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<GetRoleByIdHandler> _logger;
  public GetRoleByIdHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetRoleByIdHandler> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<RoleDTO>> Handle(GetRoleByIdQuery request, CancellationToken cancellationToken)
  {
    try
    {
      var role = await _dbContext.Roles
                              .AsNoTracking()
                              .FirstOrDefaultAsync(u => u.Id == request.RoleId, cancellationToken);

      if (role is null)
        return new(false, "Role not found");

      var roleDto = _mapper.Map<RoleDTO>(role);

      return new ApiResponse<RoleDTO>(true, "Ok", roleDto);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error fetching role {Id}", request.RoleId);
      return new(false, ex.Message);
    }
  }
}