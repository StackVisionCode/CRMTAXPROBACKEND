using CommLinkService.Application.Commands;
using CommLinkService.Infrastructure.Persistence;
using Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CommLinkService.Application.Handlers;

public sealed class DisconnectHandler : IRequestHandler<DisconnectCommand, ApiResponse<bool>>
{
    private readonly ICommLinkDbContext _context;
    private readonly ILogger<DisconnectHandler> _logger;

    public DisconnectHandler(ICommLinkDbContext context, ILogger<DisconnectHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DisconnectCommand request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var connection = await _context.Connections.FirstOrDefaultAsync(
                c => c.ConnectionId == request.ConnectionId && c.IsActive,
                cancellationToken
            );

            if (connection == null)
                return new ApiResponse<bool>(false, "Connection not found");

            connection.DisconnectedAt = DateTime.UtcNow;
            connection.IsActive = false;
            connection.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Connection {ConnectionId} disconnected", request.ConnectionId);

            return new ApiResponse<bool>(true, "Disconnected successfully", true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting");
            return new ApiResponse<bool>(false, "Failed to disconnect");
        }
    }
}
