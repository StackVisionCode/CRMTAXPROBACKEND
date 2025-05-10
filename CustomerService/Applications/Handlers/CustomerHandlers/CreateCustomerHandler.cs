using AutoMapper;
using Common;
using CustomerService.Commands.CustomerCommands;
using CustomerService.Infrastructure.Context;
using MediatR;

namespace CustomerService.Handlers.CustomerHandlers;

public class CreateCustomerHandler : IRequestHandler<CreateCustomerCommands, ApiResponse<bool>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<CreateCustomerHandler> _logger;
  public CreateCustomerHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<CreateCustomerHandler> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<bool>> Handle(CreateCustomerCommands request, CancellationToken cancellationToken)
  {
    try
    {
      var customer = _mapper.Map<Domains.Customers.Customer>(request.customer);
      customer.CreatedAt = DateTime.UtcNow;
      await _dbContext.Customers.AddAsync(customer, cancellationToken);
      var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
      _logger.LogInformation("Customer created successfully: {Customer}", customer);
      return new ApiResponse<bool>(result, result ? "Customer created successfully" : "Failed to create customer", result);
    }
    catch (Exception ex)
    {
      _logger.LogError("Error creating customer: {Message}", ex.Message);
      return new ApiResponse<bool>(false, ex.Message, false);
    }
  }
}