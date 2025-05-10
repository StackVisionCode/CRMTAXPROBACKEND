using AutoMapper;
using Common;
using CustomerService.DTOs.CustomerDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.CustomerQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.CustomerHandlers;

public class GetAllCustomerHandler : IRequestHandler<GetAllCustomerQueries, ApiResponse<List<CustomerDTO>>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<GetAllCustomerHandler> _logger;
  public GetAllCustomerHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetAllCustomerHandler> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<List<CustomerDTO>>> Handle(GetAllCustomerQueries request, CancellationToken cancellationToken)
  {
    try
    {
      var customers = await _dbContext.Customers.ToListAsync(cancellationToken);
      if (customers == null || !customers.Any())
      {
        return new ApiResponse<List<CustomerDTO>>(false, "No customers found", null!);
      }

      var customerDtos = _mapper.Map<List<CustomerDTO>>(customers);
      _logger.LogInformation("Customers retrieved successfully: {Customers}", customerDtos);
      return new ApiResponse<List<CustomerDTO>>(true, "Customers retrieved successfully", customerDtos);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving customers: {Message}", ex.Message);
      return new ApiResponse<List<CustomerDTO>>(false, ex.Message, null!);
    }
  }
}