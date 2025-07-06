using AuthService.Domains.Roles;
using AutoMapper;
using Commands.RoleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.RoleHandlers;

public class UpdateRoleHandler : IRequestHandler<UpdateRoleCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateRoleHandler> _logger;

    public UpdateRoleHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<UpdateRoleHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        UpdateRoleCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var role = await _dbContext.Roles.FirstOrDefaultAsync(
                r => r.Id == request.Role.Id,
                cancellationToken
            );

            if (role is null)
                return new(false, "Role not found", false);

            // 1 – Actualizar campos básicos
            _mapper.Map(request.Role, role);
            role.UpdatedAt = DateTime.UtcNow;

            // 2 – Permisos deseados
            var desiredPermIds = await _dbContext
                .Permissions.Where(p => request.Role.PermissionCodes.Contains(p.Code))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            // 3. IDs actuales
            var current = await _dbContext
                .RolePermissions.Where(rp => rp.RoleId == role.Id)
                .ToListAsync(cancellationToken);

            var currentIds = current.Select(rp => rp.PermissionId).ToList();

            // 4. Eliminar los que ya no están
            var toRemove = current.Where(rp => !desiredPermIds.Contains(rp.PermissionId));
            _dbContext.RolePermissions.RemoveRange(toRemove);

            // 5. Agregar nuevos
            var toAdd = desiredPermIds.Except(currentIds);
            foreach (var pid in toAdd)
            {
                _dbContext.RolePermissions.Add(
                    new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = role.Id,
                        PermissionId = pid,
                        CreatedAt = DateTime.UtcNow,
                    }
                );
            }

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0 ? true : false;

            _logger.LogInformation("Role updated successfully: {Role}", role);
            return new ApiResponse<bool>(
                result,
                result ? "Role updated successfully" : "Failed to update role",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
