using Commands.CustomerRoleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserRoleHandlers;

public class RemoveRoleFromCustomerHandler
    : IRequestHandler<RemoveRoleFromCustomerCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RemoveRoleFromCustomerHandler> _log;

    public RemoveRoleFromCustomerHandler(
        ApplicationDbContext db,
        ILogger<RemoveRoleFromCustomerHandler> log
    )
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<bool>> Handle(
        RemoveRoleFromCustomerCommand req,
        CancellationToken ct
    )
    {
        var rel = await _db.CustomerRoles.FirstOrDefaultAsync(
            ur => ur.CustomerId == req.CustomerId && ur.RoleId == req.RoleId,
            ct
        );

        if (rel is null)
            return new ApiResponse<bool>(false, "Assignment not found", false);

        _db.CustomerRoles.Remove(rel);
        var success = await _db.SaveChangesAsync(ct) > 0;
        return new ApiResponse<bool>(success, success ? "Role removed" : "Failed", success);
    }
}
