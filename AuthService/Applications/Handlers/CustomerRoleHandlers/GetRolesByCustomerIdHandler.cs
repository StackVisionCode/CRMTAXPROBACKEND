using AuthService.DTOs.RoleDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.CustomerRoleQueries;

namespace Handlers.UserRoleHandlers;

public class GetRolesByCustomerIdHandler
    : IRequestHandler<GetRolesByCustomerIdQuery, ApiResponse<List<RoleDTO>>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GetRolesByCustomerIdHandler> _log;

    public GetRolesByCustomerIdHandler(
        ApplicationDbContext db,
        ILogger<GetRolesByCustomerIdHandler> log
    )
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<List<RoleDTO>>> Handle(
        GetRolesByCustomerIdQuery req,
        CancellationToken ct
    )
    {
        var data = await (
            from ur in _db.CustomerRoles
            where ur.CustomerId == req.CustomerId
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
            _log.LogError("Customer has no roles");
            return new ApiResponse<List<RoleDTO>>(false, "Customer has no roles");
        }

        _log.LogInformation("Customer has roles");
        return new ApiResponse<List<RoleDTO>>(true, "Ok", data);
    }
}
