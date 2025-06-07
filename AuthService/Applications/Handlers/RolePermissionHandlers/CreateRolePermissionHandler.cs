using AuthService.Domains.Roles;
using AutoMapper;
using Commands.RolePermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;

namespace Handlers.RolePermissionHandlers;

public class CreateRolePermissionHanlder
    : IRequestHandler<CreateRolePermissionCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateRolePermissionHanlder> _logger;

    public CreateRolePermissionHanlder(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<CreateRolePermissionHanlder> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateRolePermissionCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var rolePermission = _mapper.Map<RolePermissions>(request.rolePermission);
            rolePermission.CreatedAt = DateTime.UtcNow;
            await _dbContext.RolePermissions.AddAsync(rolePermission, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            _logger.LogInformation(
                "Role permission created successfully: {RolePermission}",
                rolePermission
            );
            return new ApiResponse<bool>(
                result,
                result
                    ? "Role permission created successfully"
                    : "Failed to create role permission",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating role permission: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
