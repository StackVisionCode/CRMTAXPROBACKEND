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
        try
        {
            var exists = await _dbContext.Permissions.AnyAsync(
                p => p.Code == request.Permission.Code,
                cancellationToken
            );
            if (exists)
                return new(false, "Permission code already exists", false);

            var permission = _mapper.Map<Permission>(request.Permission);
            permission.CreatedAt = DateTime.UtcNow;

            /* ──────────────────────────────────────────────────────────────── */
            /*    Elegir los roles a enlazar                                   */
            /*    a) si el caller mandó RoleIds => usar esos                   */
            /*    b) caso contrario => buscar el rol “Administrator”           */
            /* ──────────────────────────────────────────────────────────────── */

            IEnumerable<Guid> targetRoles;

            if (request.Permission.RoleIds is { Count: > 0 })
            {
                targetRoles = request.Permission.RoleIds!;
            }
            else
            {
                targetRoles = await _dbContext
                    .Roles.Where(r => r.Name == "Administrator")
                    .Select(r => r.Id)
                    .ToListAsync(cancellationToken);

                if (!targetRoles.Any())
                    return new(
                        false,
                        "No target roles supplied and Administrator role not found",
                        false
                    );
            }

            /* ──────────────────────────────────────────────────────────────── */
            /* Construir RolePermission (n registros)                       */
            /* ──────────────────────────────────────────────────────────────── */
            var links = targetRoles.Select(rid => new RolePermission
            {
                Id = Guid.NewGuid(),
                RoleId = rid,
                PermissionId = permission.Id,
                CreatedAt = DateTime.UtcNow,
            });

            /* ──────────────────────────────────────────────────────────────── */
            /* Guardar todo en una sola tx                                  */
            /* ──────────────────────────────────────────────────────────────── */
            await _dbContext.Permissions.AddAsync(permission, cancellationToken);
            await _dbContext.RolePermissions.AddRangeAsync(links, cancellationToken);

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            _logger.LogInformation("Permission created successfully: {Permission}", permission);
            return new ApiResponse<bool>(
                result,
                result ? "Permission created successfully" : "Failed to create permission",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating permission: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
