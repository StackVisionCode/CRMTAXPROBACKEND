using AuthService.DTOs.RoleDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.UserRoleQueries;

namespace Handlers.UserRoleHandlers;

public class GetRolesByUserIdHandler
    : IRequestHandler<GetRolesByUserIdQuery, ApiResponse<List<RoleDTO>>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GetRolesByUserIdHandler> _log;

    public GetRolesByUserIdHandler(ApplicationDbContext db, ILogger<GetRolesByUserIdHandler> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<List<RoleDTO>>> Handle(
        GetRolesByUserIdQuery req,
        CancellationToken ct
    )
    {
        var data = await (
            from ur in _db.UserRoles
            where ur.TaxUserId == req.UserId
            join r in _db.Roles on ur.RoleId equals r.Id
            join rp in _db.RolePermissions on r.Id equals rp.RoleId into rps
            from rp in rps.DefaultIfEmpty()
            join p in _db.Permissions on rp.PermissionId equals p.Id into ps
            from p in ps.DefaultIfEmpty()

            group p by new
            {
                r.Id,
                r.Name,
                r.Description,
            } into g
            select new RoleDTO
            {
                Id = g.Key.Id,
                Name = g.Key.Name,
                Description = g.Key.Description,
                PermissionCodes = g.Where(p => p != null!).Select(p => p!.Code).Distinct().ToList(),
            }
        ).ToListAsync(ct);

        if (data is null)
        {
            _log.LogError("User has no roles");
            return new ApiResponse<List<RoleDTO>>(false, "User has no roles");
        }

        _log.LogInformation("User has roles");
        return new ApiResponse<List<RoleDTO>>(true, "Ok", data);
    }
}
