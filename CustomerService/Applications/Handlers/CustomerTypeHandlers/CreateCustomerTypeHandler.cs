using AutoMapper;
using Common;
using CustomerService.Commands.CustomerTypeCommands;
using CustomerService.Domains.Customers;
using CustomerService.Infrastructure.Context;
using MediatR;

namespace CustomerService.Handlers.CustomerTypeHandlers;

public class CreateCustomerTypeHandler : IRequestHandler<CreateCustomerTypeCommands, ApiResponse<bool>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<CreateCustomerTypeHandler> _logger;
  public CreateCustomerTypeHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<CreateCustomerTypeHandler> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<bool>> Handle(CreateCustomerTypeCommands request, CancellationToken cancellationToken)
  {
    try
    {
      var customerType = _mapper.Map<CustomerType>(request.customerType);
      customerType.CreatedAt = DateTime.UtcNow;
      await _dbContext.CustomerTypes.AddAsync(customerType, cancellationToken);
      var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
      _logger.LogInformation("Customer type created successfully: {CustomerType}", customerType);
      return new ApiResponse<bool>(result, result ? "Customer type created successfully" : "Failed to create customer type", result);
    }
    catch (Exception ex)
    {
      _logger.LogError("Error creating customer type: {Message}", ex.Message);
      return new ApiResponse<bool>(false, ex.Message, false);
    }
  }
}