using AutoMapper;
using Common;
using CustomerService.DTOs.CustomerDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.CustomerTypeQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.CustomerHandlers;

public class GetAllCustomerTypeHandlers : IRequestHandler<GetAllCustomerTypeQueries, ApiResponse<List<ReadCustomerTypeDTO>>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<GetAllCustomerTypeHandlers> _logger;
  public GetAllCustomerTypeHandlers(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetAllCustomerTypeHandlers> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }
  public async Task<ApiResponse<List<ReadCustomerTypeDTO>>> Handle(GetAllCustomerTypeQueries request, CancellationToken cancellationToken)
  {
    try
    {
      var customerTypes = await _dbContext.CustomerTypes.ToListAsync(cancellationToken);
      if (customerTypes == null || !customerTypes.Any())
      {
        return new ApiResponse<List<ReadCustomerTypeDTO>>(false, "No customer types found", null!);
      }

      var customerTypeDtos = _mapper.Map<List<ReadCustomerTypeDTO>>(customerTypes);
      _logger.LogInformation("Customer types retrieved successfully: {CustomerTypes}", customerTypeDtos);
      return new ApiResponse<List<ReadCustomerTypeDTO>>(true, "Customer types retrieved successfully", customerTypeDtos);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving customer types: {Message}", ex.Message);
      return new ApiResponse<List<ReadCustomerTypeDTO>>(false, ex.Message, null!);
    }
  }
}