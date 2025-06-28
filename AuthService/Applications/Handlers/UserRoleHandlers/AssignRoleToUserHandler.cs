using AuthService.Domains.Users;
using Commands.UserRoleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserRoleHandlers;

public class AssignRoleToUserHandler : IRequestHandler<AssignRoleToUserCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AssignRoleToUserHandler> _log;

    public AssignRoleToUserHandler(ApplicationDbContext db, ILogger<AssignRoleToUserHandler> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<bool>> Handle(AssignRoleToUserCommand req, CancellationToken ct)
    {
        // Validar existencia
        var userExists = await _db.TaxUsers.AnyAsync(u => u.Id == req.UserId, ct);
        if (!userExists)
            return new ApiResponse<bool>(false, "User not found", false);

        var roleExists = await _db.Roles.AnyAsync(r => r.Id == req.RoleId, ct);
        if (!roleExists)
            return new ApiResponse<bool>(false, "Role not found", false);

        var already = await _db.UserRoles.AnyAsync(
            ur => ur.TaxUserId == req.UserId && ur.RoleId == req.RoleId,
            ct
        );

        if (already)
            return new ApiResponse<bool>(false, "User already has this role", false);

        await _db.UserRoles.AddAsync(
            new UserRole
            {
                Id = Guid.NewGuid(),
                TaxUserId = req.UserId,
                RoleId = req.RoleId,
                CreatedAt = DateTime.UtcNow,
            },
            ct
        );

        var success = await _db.SaveChangesAsync(ct) > 0;
        return new ApiResponse<bool>(success, success ? "Role assigned" : "Failed", success);
    }
}
