using AuthService.Domains.Permissions;
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
            await _dbContext.Permissions.AddAsync(permission, cancellationToken);
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
