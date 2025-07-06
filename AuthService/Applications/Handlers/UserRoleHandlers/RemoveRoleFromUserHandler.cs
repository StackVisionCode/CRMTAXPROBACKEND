using Commands.UserRoleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserRoleHandlers;

public class RemoveRoleFromUserHandler
    : IRequestHandler<RemoveRoleFromUserCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RemoveRoleFromUserHandler> _log;

    public RemoveRoleFromUserHandler(
        ApplicationDbContext db,
        ILogger<RemoveRoleFromUserHandler> log
    )
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<bool>> Handle(RemoveRoleFromUserCommand req, CancellationToken ct)
    {
        var rel = await _db.UserRoles.FirstOrDefaultAsync(
            ur => ur.TaxUserId == req.UserId && ur.RoleId == req.RoleId,
            ct
        );
        if (rel is null)
            return new ApiResponse<bool>(false, "Assignment not found", false);

        _db.UserRoles.Remove(rel);
        var success = await _db.SaveChangesAsync(ct) > 0;
        return new ApiResponse<bool>(success, success ? "Role removed" : "Failed", success);
    }
}
