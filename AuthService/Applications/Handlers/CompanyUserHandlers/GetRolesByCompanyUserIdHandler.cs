using AuthService.DTOs.RoleDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CompanyUserQueries;

namespace Handlers.CompanyUserHandlers;

public class GetRolesByCompanyUserIdHandler
    : IRequestHandler<GetRolesByCompanyUserIdQuery, ApiResponse<List<RoleDTO>>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GetRolesByCompanyUserIdHandler> _log;

    public GetRolesByCompanyUserIdHandler(
        ApplicationDbContext db,
        ILogger<GetRolesByCompanyUserIdHandler> log
    )
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<List<RoleDTO>>> Handle(
        GetRolesByCompanyUserIdQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var roles = await (
                from cur in _db.CompanyUserRoles
                where cur.CompanyUserId == request.CompanyUserId
                join r in _db.Roles on cur.RoleId equals r.Id
                join rp in _db.RolePermissions on r.Id equals rp.RoleId into rps
                from rp in rps.DefaultIfEmpty()
                join p in _db.Permissions on rp.PermissionId equals p.Id into ps
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
                    PermissionCodes = g.Where(p => p != null!)
                        .Select(p => p!.Code)
                        .Distinct()
                        .ToList(),
                }
            ).ToListAsync(cancellationToken);

            if (!roles.Any())
            {
                _log.LogWarning(
                    "No roles found for company user {CompanyUserId}",
                    request.CompanyUserId
                );
                return new ApiResponse<List<RoleDTO>>(false, "No roles found for company user");
            }

            _log.LogInformation(
                "Retrieved {Count} roles for company user {CompanyUserId}",
                roles.Count,
                request.CompanyUserId
            );
            return new ApiResponse<List<RoleDTO>>(true, "Roles retrieved successfully", roles);
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Error retrieving roles for company user {CompanyUserId}",
                request.CompanyUserId
            );
            return new ApiResponse<List<RoleDTO>>(false, "Error retrieving roles");
        }
    }
}
