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
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var role = await _dbContext.Roles.FirstOrDefaultAsync(
                r => r.Id == request.Role.Id,
                cancellationToken
            );

            if (role is null)
            {
                _logger.LogWarning("Role not found: {RoleId}", request.Role.Id);
                return new ApiResponse<bool>(false, "Role not found", false);
            }

            // Verificar si el nombre ya existe en otro rol
            if (request.Role.Name != role.Name)
            {
                var nameExistsQuery =
                    from r in _dbContext.Roles
                    where r.Name == request.Role.Name && r.Id != request.Role.Id
                    select r.Id;

                if (await nameExistsQuery.AnyAsync(cancellationToken))
                {
                    _logger.LogWarning("Role name already exists: {RoleName}", request.Role.Name);
                    return new ApiResponse<bool>(false, "Role name already exists", false);
                }
            }

            // Actualizar campos básicos (incluye ServiceLevel automáticamente via mapper)
            _mapper.Map(request.Role, role);
            role.UpdatedAt = DateTime.UtcNow;

            // Manejar permisos si se especifican
            if (request.Role.PermissionCodes?.Any() == true)
            {
                // Validar permisos
                var validPermissionsQuery =
                    from p in _dbContext.Permissions
                    where request.Role.PermissionCodes.Contains(p.Code)
                    select p.Id;

                var desiredPermIds = await validPermissionsQuery.ToListAsync(cancellationToken);

                if (desiredPermIds.Count != request.Role.PermissionCodes.Count)
                {
                    var invalidCodes = request
                        .Role.PermissionCodes.Except(
                            await _dbContext
                                .Permissions.Where(p =>
                                    request.Role.PermissionCodes.Contains(p.Code)
                                )
                                .Select(p => p.Code)
                                .ToListAsync(cancellationToken)
                        )
                        .ToList();

                    _logger.LogWarning(
                        "Invalid permission codes: {InvalidCodes}",
                        string.Join(", ", invalidCodes)
                    );
                    return new ApiResponse<bool>(
                        false,
                        $"Invalid permission codes: {string.Join(", ", invalidCodes)}",
                        false
                    );
                }

                // Permisos actuales
                var currentRolePermissions = await _dbContext
                    .RolePermissions.Where(rp => rp.RoleId == role.Id)
                    .ToListAsync(cancellationToken);

                var currentIds = currentRolePermissions.Select(rp => rp.PermissionId).ToList();

                // Eliminar los que ya no están
                var toRemove = currentRolePermissions.Where(rp =>
                    !desiredPermIds.Contains(rp.PermissionId)
                );
                _dbContext.RolePermissions.RemoveRange(toRemove);

                // Agregar nuevos
                var toAdd = desiredPermIds.Except(currentIds);
                foreach (var pid in toAdd)
                {
                    await _dbContext.RolePermissions.AddAsync(
                        new RolePermission
                        {
                            Id = Guid.NewGuid(),
                            RoleId = role.Id,
                            PermissionId = pid,
                            CreatedAt = DateTime.UtcNow,
                        },
                        cancellationToken
                    );
                }

                _logger.LogDebug(
                    "Updated role permissions: Removed={RemovedCount}, Added={AddedCount}",
                    toRemove.Count(),
                    toAdd.Count()
                );
            }

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Role updated successfully: {RoleName} (ServiceLevel: {ServiceLevel})",
                    role.Name,
                    role.ServiceLevel
                );
                return new ApiResponse<bool>(true, "Role updated successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to update role: {RoleId}", request.Role.Id);
                return new ApiResponse<bool>(false, "Failed to update role", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error updating role: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
