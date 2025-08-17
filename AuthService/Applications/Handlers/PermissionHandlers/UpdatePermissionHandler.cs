using AutoMapper;
using Commands.PermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.PermissionHandlers;

public class UpdatePermissionHandler : IRequestHandler<UpdatePermissionCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdatePermissionHandler> _logger;

    public UpdatePermissionHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<UpdatePermissionHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        UpdatePermissionCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var permission = await _dbContext.Permissions.FindAsync(
                new object[] { request.Permission.Id },
                cancellationToken
            );
            if (permission == null)
            {
                _logger.LogWarning("Permission not found: {PermissionId}", request.Permission.Id);
                return new ApiResponse<bool>(false, "Permission not found", false);
            }

            // Verificar si el código ya existe en otro permiso (si se está cambiando)
            if (request.Permission.Code != permission.Code)
            {
                var codeExistsQuery =
                    from p in _dbContext.Permissions
                    where p.Code == request.Permission.Code && p.Id != request.Permission.Id
                    select p.Id;

                if (await codeExistsQuery.AnyAsync(cancellationToken))
                {
                    _logger.LogWarning(
                        "Permission code already exists: {Code}",
                        request.Permission.Code
                    );
                    return new ApiResponse<bool>(false, "Permission code already exists", false);
                }
            }

            _mapper.Map(request.Permission, permission);
            permission.UpdatedAt = DateTime.UtcNow;

            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                _logger.LogInformation("Permission updated successfully: {Code}", permission.Code);
                return new ApiResponse<bool>(true, "Permission updated successfully", true);
            }
            else
            {
                _logger.LogError(
                    "Failed to update permission: {PermissionId}",
                    request.Permission.Id
                );
                return new ApiResponse<bool>(false, "Failed to update permission", false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
