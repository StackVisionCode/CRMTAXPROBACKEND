using AuthService.DTOs.PermissionDTOs;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Queries.PermissionQueries;

namespace Handlers.PermissionHandlers;

public class GetUserPermissionsHandler
    : IRequestHandler<GetUserPermissionsQuery, ApiResponse<UserPermissionsDTO>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<GetUserPermissionsHandler> _log;

    public GetUserPermissionsHandler(
        ApplicationDbContext db,
        ILogger<GetUserPermissionsHandler> log
    )
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<UserPermissionsDTO>> Handle(
        GetUserPermissionsQuery req,
        CancellationToken ct
    )
    {
        var codes = await (
            from ur in _db.UserRoles
            where ur.TaxUserId == req.UserId
            join rp in _db.RolePermissions on ur.RoleId equals rp.RoleId
            join p in _db.Permissions on rp.PermissionId equals p.Id
            select p.Code
        )
            .Distinct()
            .ToListAsync(ct);

        if (!codes.Any())
            return new ApiResponse<UserPermissionsDTO>(false, "No permissions found for user");

        var dto = new UserPermissionsDTO { UserId = req.UserId, PermissionCodes = codes };
        return new ApiResponse<UserPermissionsDTO>(true, "ok", dto);
    }
}
