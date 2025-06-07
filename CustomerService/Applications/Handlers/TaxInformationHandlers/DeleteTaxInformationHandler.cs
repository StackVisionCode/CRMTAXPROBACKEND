using Common;
using CustomerService.Commands.TaxInformationCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.TaxInformationHandlers;

public class DeleteTaxInformationHandler
    : IRequestHandler<DeleteTaxInformationCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteTaxInformationHandler> _logger;

    public DeleteTaxInformationHandler(
        ApplicationDbContext dbContext,
        ILogger<DeleteTaxInformationHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteTaxInformationCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var taxInfo = await _dbContext.TaxInformations.FirstOrDefaultAsync(
                c => c.Id == request.Id,
                cancellationToken
            );

            if (taxInfo == null)
            {
                _logger.LogWarning(
                    "Tax information with ID {Id} not found for deletion",
                    request.Id
                );
                return new ApiResponse<bool>(false, "Tax information not found", false);
            }

            _dbContext.TaxInformations.Remove(taxInfo);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            if (result)
            {
                _logger.LogInformation(
                    "Tax information with ID {Id} deleted successfully",
                    request.Id
                );
                return new ApiResponse<bool>(true, "Tax information deleted successfully", true);
            }
            else
            {
                _logger.LogWarning("Failed to delete tax information with ID {Id}", request.Id);
                return new ApiResponse<bool>(false, "Failed to delete tax information", false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An error occurred while deleting tax information with ID {Id}",
                request.Id
            );
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
