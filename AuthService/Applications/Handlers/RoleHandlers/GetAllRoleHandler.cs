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

    public GetAllRoleHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<GetAllRoleHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<RoleDTO>>> Handle(
        GetAllRoleQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // JOIN Roles ↔ RolePermissions ↔ Permissions
            var roles = await (
                from r in _dbContext.Roles
                join rp in _dbContext.RolePermissions on r.Id equals rp.RoleId into rps
                from rp in rps.DefaultIfEmpty()
                join p in _dbContext.Permissions on rp.PermissionId equals p.Id into ps
                from p in ps.DefaultIfEmpty()

                group p by new
                {
                    r.Id,
                    r.Name,
                    r.Description,
                    r.PortalAccess,
                } into g
                select new RoleDTO
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    Description = g.Key.Description,
                    PortalAccess = g.Key.PortalAccess,
                    PermissionCodes = g.Where(p => p != null)
                        .Select(p => p!.Code)
                        .Distinct()
                        .ToList(),
                }
            ).ToListAsync(cancellationToken);

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
