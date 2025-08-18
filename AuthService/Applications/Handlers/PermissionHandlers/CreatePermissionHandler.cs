using AuthService.Domains.Permissions;
using AuthService.Domains.Roles;
using AutoMapper;
using Commands.PermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.PermissionHandlers;

public class CreatePermissionHandler : IRequestHandler<CreatePermissionCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CreatePermissionHandler> _logger;

    public CreatePermissionHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<CreatePermissionHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreatePermissionCommands request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Verificar si el código ya existe
            var exists = await _dbContext.Permissions.AnyAsync(
                p => p.Code == request.Permission.Code,
                cancellationToken
            );
            if (exists)
            {
                _logger.LogWarning(
                    "Permission code already exists: {Code}",
                    request.Permission.Code
                );
                return new ApiResponse<bool>(false, "Permission code already exists", false);
            }

            var permission = _mapper.Map<Permission>(request.Permission);
            permission.Id = Guid.NewGuid();
            permission.CreatedAt = DateTime.UtcNow;

            // Elegir los roles a enlazar
            IEnumerable<Guid> targetRoles;

            if (request.Permission.RoleIds is { Count: > 0 })
            {
                // Verificar que los roles existan
                var validRolesQuery =
                    from r in _dbContext.Roles
                    where request.Permission.RoleIds.Contains(r.Id)
                    select r.Id;

                var validRoles = await validRolesQuery.ToListAsync(cancellationToken);
                if (validRoles.Count != request.Permission.RoleIds.Count)
                {
                    var invalidIds = request.Permission.RoleIds.Except(validRoles);
                    _logger.LogWarning(
                        "Invalid role IDs: {InvalidIds}",
                        string.Join(", ", invalidIds)
                    );
                    return new ApiResponse<bool>(false, "One or more role IDs are invalid", false);
                }

                targetRoles = validRoles;
            }
            else
            {
                // Buscar TODOS los roles Administrator (Basic, Standard, Pro)
                var adminRolesQuery =
                    from r in _dbContext.Roles
                    where r.Name.Contains("Administrator") || r.Name == "Developer"
                    select r.Id;

                targetRoles = await adminRolesQuery.ToListAsync(cancellationToken);

                if (!targetRoles.Any())
                {
                    _logger.LogWarning("No Administrator roles found to assign permission to");
                    return new ApiResponse<bool>(
                        false,
                        "No Administrator roles found and no target roles supplied",
                        false
                    );
                }

                _logger.LogDebug(
                    "Assigning permission to {Count} Administrator roles",
                    targetRoles.Count()
                );
            }

            // Construir RolePermission links
            var links = targetRoles
                .Select(rid => new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = rid,
                    PermissionId = permission.Id,
                    CreatedAt = DateTime.UtcNow,
                })
                .ToList();

            // Guardar todo
            await _dbContext.Permissions.AddAsync(permission, cancellationToken);
            await _dbContext.RolePermissions.AddRangeAsync(links, cancellationToken);

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Permission created successfully: {Code} assigned to {RoleCount} roles",
                    permission.Code,
                    links.Count
                );
                return new ApiResponse<bool>(true, "Permission created successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to create permission: {Code}", request.Permission.Code);
                return new ApiResponse<bool>(false, "Failed to create permission", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating permission: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
