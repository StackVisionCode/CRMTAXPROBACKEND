using AuthService.Domains.CompanyUsers;
using Commands.CompanyUserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.CompanyUserHandlers;

public class AssignRoleToCompanyUserHandler
    : IRequestHandler<AssignRoleToCompanyUserCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AssignRoleToCompanyUserHandler> _log;

    public AssignRoleToCompanyUserHandler(
        ApplicationDbContext db,
        ILogger<AssignRoleToCompanyUserHandler> log
    )
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<bool>> Handle(
        AssignRoleToCompanyUserCommand req,
        CancellationToken ct
    )
    {
        // Validar existencia del usuario
        var userExists = await _db.CompanyUsers.AnyAsync(u => u.Id == req.CompanyUserId, ct);
        if (!userExists)
            return new ApiResponse<bool>(false, "Company user not found", false);

        // Validar existencia del rol
        var roleExists = await _db.Roles.AnyAsync(r => r.Id == req.RoleId, ct);
        if (!roleExists)
            return new ApiResponse<bool>(false, "Role not found", false);

        // Verificar si ya tiene el rol
        var already = await _db.CompanyUserRoles.AnyAsync(
            cur => cur.CompanyUserId == req.CompanyUserId && cur.RoleId == req.RoleId,
            ct
        );

        if (already)
            return new ApiResponse<bool>(false, "Company user already has this role", false);

        await _db.CompanyUserRoles.AddAsync(
            new CompanyUserRole
            {
                Id = Guid.NewGuid(),
                CompanyUserId = req.CompanyUserId,
                RoleId = req.RoleId,
                CreatedAt = DateTime.UtcNow,
            },
            ct
        );

        var success = await _db.SaveChangesAsync(ct) > 0;
        return new ApiResponse<bool>(success, success ? "Role assigned" : "Failed", success);
    }
}
