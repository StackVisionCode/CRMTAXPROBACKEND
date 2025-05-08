using AutoMapper;
using Common;
using CustomerService.DTOs.CustomerDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.CustomerTypeQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.CustomerHandlers;

public class GetAllCustomerTypeHandler : IRequestHandler<GetAllCustomerTypeQueries, ApiResponse<List<CustomerTypeDTO>>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<GetAllCustomerTypeHandler> _logger;
  public GetAllCustomerTypeHandler(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetAllCustomerTypeHandler> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<List<CustomerTypeDTO>>> Handle(GetAllCustomerTypeQueries request, CancellationToken cancellationToken)
  {
    try
    {
      var customerTypes = await _dbContext.CustomerTypes.ToListAsync(cancellationToken);
      if (customerTypes == null || !customerTypes.Any())
      {
        return new ApiResponse<List<CustomerTypeDTO>>(false, "No customer types found", null!);
      }

      var customerTypeDtos = _mapper.Map<List<CustomerTypeDTO>>(customerTypes);
      _logger.LogInformation("Customer types retrieved successfully: {CustomerTypes}", customerTypeDtos);
      return new ApiResponse<List<CustomerTypeDTO>>(true, "Customer types retrieved successfully", customerTypeDtos);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving customer types: {Message}", ex.Message);
      return new ApiResponse<List<CustomerTypeDTO>>(false, ex.Message, null!);
    }
  }
}