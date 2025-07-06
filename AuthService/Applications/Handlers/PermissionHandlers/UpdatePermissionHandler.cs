using AutoMapper;
using Commands.PermissionCommands;
using Common;
using Infraestructure.Context;
using MediatR;

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
                return new ApiResponse<bool>(false, "Permission not found", false);
            }

            _mapper.Map(request.Permission, permission);
            permission.UpdatedAt = DateTime.UtcNow;
            _dbContext.Permissions.Update(permission);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0 ? true : false;
            _logger.LogInformation("Permission updated successfully: {Permission}", permission);
            return new ApiResponse<bool>(
                result,
                result ? "Permission updated successfully" : "Failed to update permission",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating permission: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
