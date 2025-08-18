using AutoMapper;
using Common;
using CustomerService.Commands.CustomerCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.CustomerHandlers;

public class UpdateCustomerHandler : IRequestHandler<UpdateCustomerCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<UpdateCustomerHandler> _logger;

    public UpdateCustomerHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<UpdateCustomerHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<bool>> Handle(
        UpdateCustomerCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var existingCustomer = await _dbContext.Customers.FirstOrDefaultAsync(
                c => c.Id == request.customer.Id,
                cancellationToken
            );

            if (existingCustomer == null)
            {
                _logger.LogWarning(
                    "Customer with ID {Id} not found for update",
                    request.customer.Id
                );
                return new ApiResponse<bool>(false, "Customer not found", false);
            }

            // Verificar si otro customer ya tiene el mismo SSN/ITIN en la misma company
            var duplicateExists = await _dbContext.Customers.AnyAsync(
                c =>
                    c.SsnOrItin == request.customer.SsnOrItin
                    && c.Id != request.customer.Id
                    && c.CompanyId == existingCustomer.CompanyId,
                cancellationToken
            );

            if (duplicateExists)
            {
                _logger.LogWarning(
                    "Another customer already exists with SSN/ITIN: {SsnOrItin} in Company: {CompanyId}",
                    request.customer.SsnOrItin,
                    existingCustomer.CompanyId
                );
                return new ApiResponse<bool>(
                    false,
                    "Another customer with this SSN or ITIN already exists in your company.",
                    false
                );
            }

            // Mapear los nuevos valores al customer existente
            _mapper.Map(request.customer, existingCustomer);
            existingCustomer.UpdatedAt = DateTime.UtcNow;

            _dbContext.Customers.Update(existingCustomer);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                _logger.LogInformation(
                    "Customer updated successfully: {CustomerId} by TaxUser: {LastModifiedBy}",
                    existingCustomer.Id,
                    existingCustomer.LastModifiedByTaxUserId
                );
                return new ApiResponse<bool>(true, "Customer updated successfully", true);
            }
            else
            {
                _logger.LogWarning("Failed to update customer: {CustomerId}", existingCustomer.Id);
                return new ApiResponse<bool>(false, "Failed to update customer", false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
