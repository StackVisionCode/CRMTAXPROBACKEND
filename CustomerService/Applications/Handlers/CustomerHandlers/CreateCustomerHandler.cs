using AutoMapper;
using Common;
using CustomerService.Commands.CustomerCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SharedLibrary.Contracts;
using SharedLibrary.DTOs.CustomerEventsDTO;

namespace CustomerService.Handlers.CustomerHandlers;

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommands, ApiResponse<bool>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<CreateCustomerHandler> _logger;
    private readonly IEventBus _eventBus;

    public CreateCustomerHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<CreateCustomerHandler> logger,
        IEventBus eventBus
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
        _eventBus = eventBus;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateCustomerCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var exists = await _dbContext.Customers.AnyAsync(
                c =>
                    c.SsnOrItin == request.customer.SsnOrItin
                    && c.CompanyId == request.customer.CompanyId,
                cancellationToken
            );

            if (exists)
            {
                _logger.LogWarning(
                    "Customer already exists with SSN/ITIN: {SsnOrItin} in Company: {CompanyId}",
                    request.customer.SsnOrItin,
                    request.customer.CompanyId
                );
                return new ApiResponse<bool>(
                    false,
                    "Customer with this SSN or ITIN already exists in your company.",
                    false
                );
            }

            var customer = _mapper.Map<Domains.Customers.Customer>(request.customer);
            customer.CreatedAt = DateTime.UtcNow;

            await _dbContext.Customers.AddAsync(customer, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;

            if (result)
            {
                // Publish the customer created event
                var customerCreatedEvent = new CustomerCreatedEvent(
                    Id: Guid.NewGuid(),
                    OccurredOn: DateTime.UtcNow,
                    CustomerId: customer.Id,
                    TaxUserId: customer.CompanyId,
                    FirstName: customer.FirstName,
                    MiddleName: customer.MiddleName,
                    LastName: customer.LastName,
                    Folders: new[] { "Documents", "Firms", "Requests" }
                );

                _eventBus.Publish(customerCreatedEvent);
            }
            _logger.LogInformation(
                "Customer created successfully: {CustomerId} by TaxUser: {CreatedBy} in Company: {CompanyId}",
                customer.Id,
                customer.CreatedByTaxUserId,
                customer.CompanyId
            );

            return new ApiResponse<bool>(
                result,
                result ? "Customer created successfully" : "Failed to create customer",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating customer: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
