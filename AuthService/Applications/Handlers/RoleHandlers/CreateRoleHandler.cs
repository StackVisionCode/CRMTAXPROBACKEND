using AuthService.Domains.Roles;
using AutoMapper;
using Commands.RoleCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.RoleHandlers;

public class CreateRoleHandler : IRequestHandler<CreateRoleCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateRoleHandler> _logger;

    public CreateRoleHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<CreateRoleHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateRoleCommands request,
        CancellationToken cancellationToken
    )
    {
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Validar que el nombre del rol no exista
            var nameExistsQuery =
                from r in _dbContext.Roles
                where r.Name == request.Role.Name
                select r.Id;

            if (await nameExistsQuery.AnyAsync(cancellationToken))
            {
                _logger.LogWarning("Role name already exists: {RoleName}", request.Role.Name);
                return new ApiResponse<bool>(false, "Role name already exists", false);
            }

            // Mapear con todos los campos
            var role = _mapper.Map<Role>(request.Role);
            role.Id = Guid.NewGuid();
            role.CreatedAt = DateTime.UtcNow;

            // Validar permisos existentes
            if (request.Role.PermissionCodes?.Any() == true)
            {
                var validPermissionsQuery =
                    from p in _dbContext.Permissions
                    where request.Role.PermissionCodes.Contains(p.Code)
                    select new { p.Id, p.Code };

                var validPermissions = await validPermissionsQuery.ToListAsync(cancellationToken);

                var invalidCodes = request
                    .Role.PermissionCodes.Except(validPermissions.Select(vp => vp.Code))
                    .ToList();
                if (invalidCodes.Any())
                {
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

                // Crear objetos de unión
                role.RolePermissions = validPermissions
                    .Select(vp => new RolePermission
                    {
                        Id = Guid.NewGuid(),
                        RoleId = role.Id,
                        PermissionId = vp.Id,
                        CreatedAt = DateTime.UtcNow,
                    })
                    .ToList();

                _logger.LogDebug(
                    "Created role with {PermissionCount} permissions",
                    validPermissions.Count
                );
            }

            await _dbContext.Roles.AddAsync(role, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                await transaction.CommitAsync(cancellationToken);
                _logger.LogInformation(
                    "Role created successfully: {RoleName} (ServiceLevel: {ServiceLevel})",
                    role.Name,
                    role.ServiceLevel
                );
                return new ApiResponse<bool>(true, "Role created successfully", true);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogError("Failed to create role: {RoleName}", request.Role.Name);
                return new ApiResponse<bool>(false, "Failed to create role", false);
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error creating role: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
