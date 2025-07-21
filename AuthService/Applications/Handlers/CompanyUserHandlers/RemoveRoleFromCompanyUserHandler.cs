using Commands.CompanyUserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.CompanyUserHandlers;

public class RemoveRoleFromCompanyUserHandler
    : IRequestHandler<RemoveRoleFromCompanyUserCommand, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<RemoveRoleFromCompanyUserHandler> _log;

    public RemoveRoleFromCompanyUserHandler(
        ApplicationDbContext db,
        ILogger<RemoveRoleFromCompanyUserHandler> log
    )
    {
        _db = db;
        _log = log;
    }

    public async Task<ApiResponse<bool>> Handle(
        RemoveRoleFromCompanyUserCommand req,
        CancellationToken ct
    )
    {
        try
        {
            // Validar que el usuario existe
            var userExists = await _db.CompanyUsers.AnyAsync(u => u.Id == req.CompanyUserId, ct);
            if (!userExists)
                return new ApiResponse<bool>(false, "Company user not found", false);

            // Buscar la relación usuario-rol
            var userRole = await _db.CompanyUserRoles.FirstOrDefaultAsync(
                cur => cur.CompanyUserId == req.CompanyUserId && cur.RoleId == req.RoleId,
                ct
            );

            if (userRole is null)
                return new ApiResponse<bool>(false, "Company user does not have this role", false);

            // Verificar que no sea el último rol (opcional - puedes comentar si quieres permitir usuarios sin roles)
            var roleCount = await _db.CompanyUserRoles.CountAsync(
                cur => cur.CompanyUserId == req.CompanyUserId,
                ct
            );
            if (roleCount <= 1)
                return new ApiResponse<bool>(
                    false,
                    "Cannot remove the last role from company user",
                    false
                );

            _db.CompanyUserRoles.Remove(userRole);
            var success = await _db.SaveChangesAsync(ct) > 0;

            _log.LogInformation(
                "Role {RoleId} removed from company user {CompanyUserId}",
                req.RoleId,
                req.CompanyUserId
            );
            return new ApiResponse<bool>(
                success,
                success ? "Role removed successfully" : "Failed to remove role",
                success
            );
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Error removing role from company user");
            return new ApiResponse<bool>(false, "Error removing role from company user", false);
        }
    }
}
