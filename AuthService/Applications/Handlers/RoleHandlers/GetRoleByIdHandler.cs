using AuthService.DTOs.RoleDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.RoleQueries;

namespace Handlers.RoleHandlers;

public class GetRoleByIdHandler : IRequestHandler<GetRoleByIdQuery, ApiResponse<RoleDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<GetRoleByIdHandler> _logger;

    public GetRoleByIdHandler(ApplicationDbContext dbContext, ILogger<GetRoleByIdHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<RoleDTO>> Handle(
        GetRoleByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Query con ServiceLevel y CreatedAt
            var role = await (
                from r in _dbContext.Roles
                where r.Id == request.RoleId
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
                    r.ServiceLevel,
                    r.CreatedAt,
                } into g
                select new RoleDTO
                {
                    Id = g.Key.Id,
                    Name = g.Key.Name,
                    Description = g.Key.Description,
                    PortalAccess = g.Key.PortalAccess,
                    ServiceLevel = g.Key.ServiceLevel,
                    CreatedAt = g.Key.CreatedAt,
                    PermissionCodes = g.Where(p => p != null)
                        .Select(p => p!.Code)
                        .Distinct()
                        .ToList(),
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (role is null)
            {
                _logger.LogWarning("Role not found: {RoleId}", request.RoleId);
                return new ApiResponse<RoleDTO>(false, "Role not found", null!);
            }

            _logger.LogInformation("Role retrieved successfully: {RoleId}", request.RoleId);
            return new ApiResponse<RoleDTO>(true, "Role retrieved successfully", role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching role {Id}: {Message}", request.RoleId, ex.Message);
            return new ApiResponse<RoleDTO>(false, ex.Message, null!);
        }
    }
}
