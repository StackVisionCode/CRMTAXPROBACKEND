using Commands.UserCommands;
using Common;
using Infraestructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Handlers.UserTaxHandlers;

public class DeleteUserTaxHandler : IRequestHandler<DeleteTaxUserCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteUserTaxHandler> _logger;
    public DeleteUserTaxHandler(ApplicationDbContext dbContext, ILogger<DeleteUserTaxHandler> logger)
    {
        _dbContext = dbContext;   
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(DeleteTaxUserCommands request, CancellationToken cancellationToken)
    {
                try
                {
                    var userTax = await _dbContext.TaxUsers.Include(u => u.Sessions)
                                                            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);
                    if (userTax == null)
                    {
                        return new ApiResponse<bool>(false, "User tax not found", false);
                    }

                    _dbContext.RemoveRange(userTax.Sessions);
                    _dbContext.TaxUsers.Remove(userTax);
                    var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0 ? true : false;
                    _logger.LogInformation("User tax deleted successfully: {Id}", request.Id);
                    return new ApiResponse<bool>(result, result ? "User tax deleted successfully" : "Failed to delete user tax", result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting user tax: {UserTax}", request.Id);
                    return new ApiResponse<bool>(false, "An error occurred while deleting the user tax", false);
                    
                }
    }
}