using Common;
using CustomerService.Commands.ContactInfoCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.ContactInfoHandlers;

public class DeleteContactInfoHandler
    : IRequestHandler<DeleteContactInfoCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteContactInfoHandler> _logger;

    public DeleteContactInfoHandler(
        ApplicationDbContext dbContext,
        ILogger<DeleteContactInfoHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteContactInfoCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var contactInfo = await _dbContext.ContactInfos.FirstOrDefaultAsync(
                c => c.Id == request.Id,
                cancellationToken
            );

            if (contactInfo == null)
            {
                _logger.LogWarning("ContactInfo with ID {Id} not found for deletion", request.Id);
                return new ApiResponse<bool>(false, "ContactInfo not found", false);
            }

            _dbContext.ContactInfos.Remove(contactInfo);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                _logger.LogInformation("ContactInfo with ID {Id} deleted successfully", request.Id);
                return new ApiResponse<bool>(true, "ContactInfo deleted successfully", true);
            }
            else
            {
                _logger.LogWarning("Failed to delete ContactInfo with ID {Id}", request.Id);
                return new ApiResponse<bool>(false, "Failed to delete ContactInfo", false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error occurred while deleting ContactInfo with ID {Id}",
                request.Id
            );
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
