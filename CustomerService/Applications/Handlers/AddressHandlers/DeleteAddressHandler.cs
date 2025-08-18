using Common;
using CustomerService.DTOs.AddressCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.AddressHandlers;

public class DeleteAddressHandler : IRequestHandler<DeleteAddressCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteAddressHandler> _logger;

    public DeleteAddressHandler(
        ApplicationDbContext dbContext,
        ILogger<DeleteAddressHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteAddressCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var address = await _dbContext.Addresses.FirstOrDefaultAsync(
                c => c.Id == request.Id,
                cancellationToken
            );

            if (address == null)
            {
                _logger.LogWarning("Address with ID {Id} not found for deletion", request.Id);
                return new ApiResponse<bool>(false, "Address not found", false);
            }

            // Soft delete - solo actualizar timestamp
            address.UpdatedAt = DateTime.UtcNow;

            _dbContext.Addresses.Update(address);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                _logger.LogInformation(
                    "Address soft deleted successfully: {AddressId}",
                    address.Id
                );
                return new ApiResponse<bool>(true, "Address soft deleted successfully", true);
            }
            else
            {
                _logger.LogWarning("Failed to soft delete Address: {AddressId}", address.Id);
                return new ApiResponse<bool>(false, "Failed to soft delete Address", false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while soft deleting Address with ID {Id}",
                request.Id
            );
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
