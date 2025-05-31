using Common;
using CustomerService.Commands.CustomerCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.CustomerHandlers;

public class DeleteCustomerHandler : IRequestHandler<DeleteCustomerCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<DeleteCustomerHandler> _logger;

    public DeleteCustomerHandler(
        ApplicationDbContext dbContext,
        ILogger<DeleteCustomerHandler> logger
    )
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        DeleteCustomerCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var customer = await _dbContext.Customers.FirstOrDefaultAsync(
                c => c.Id == request.Id,
                cancellationToken
            );

            if (customer == null)
            {
                _logger.LogWarning("Customer with ID {Id} not found for deletion", request.Id);
                return new ApiResponse<bool>(false, "Customer not found", false);
            }

            // Soft delete - marcar como inactivo en lugar de eliminar físicamente
            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;

            _dbContext.Customers.Update(customer);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                _logger.LogInformation(
                    "Customer soft deleted successfully: {CustomerId}",
                    customer.Id
                );
                return new ApiResponse<bool>(true, "Customer deleted successfully", true);
            }
            else
            {
                _logger.LogWarning("Failed to delete customer: {CustomerId}", customer.Id);
                return new ApiResponse<bool>(false, "Failed to delete customer", false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
