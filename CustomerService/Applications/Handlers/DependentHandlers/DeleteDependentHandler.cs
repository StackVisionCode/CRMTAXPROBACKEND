using Common;
using CustomerService.Commands.DependentCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.DependentHandlers;

public class DeleteDependentHandler : IRequestHandler<DeleteDependetCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteDependentHandler> _logger;

    public DeleteDependentHandler(
        ApplicationDbContext dbContext,
        ILogger<DeleteDependentHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteDependetCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var dependent = await _dbContext.Dependents.FirstOrDefaultAsync(
                d => d.Id == request.Id,
                cancellationToken
            );

            if (dependent == null)
            {
                _logger.LogWarning("Dependent with ID {Id} not found", request.Id);
                return new ApiResponse<bool>(false, "Dependent not found", false);
            }

            _dbContext.Dependents.Remove(dependent);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                _logger.LogInformation("Dependent with ID {Id} deleted successfully", request.Id);
                return new ApiResponse<bool>(
                    true,
                    "Dependent deleted successfully: {Dependent}",
                    true
                );
            }
            else
            {
                _logger.LogWarning("Failed to delete dependent with ID {Id}", request.Id);
                return new ApiResponse<bool>(false, "Failed to delete dependent", false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dependent with ID {Id}", request.Id);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
