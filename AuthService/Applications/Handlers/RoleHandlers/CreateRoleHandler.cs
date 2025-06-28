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
        try
        {
            var role = _mapper.Map<Role>(request.Role);
            role.CreatedAt = DateTime.UtcNow;

            var permIds = await _dbContext
                .Permissions.Where(p => request.Role.PermissionCodes.Contains(p.Code))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            // 3. Crear objetos de unión
            role.RolePermissions = permIds
                .Select(id => new RolePermission
                {
                    Id = Guid.NewGuid(),
                    RoleId = role.Id,
                    PermissionId = id,
                    CreatedAt = DateTime.UtcNow,
                })
                .ToList();

            await _dbContext.Roles.AddAsync(role, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            _logger.LogInformation("Role created successfully: {Role}", role);
            return new ApiResponse<bool>(
                result,
                result ? "Role created successfully" : "Failed to create role",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating role: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
