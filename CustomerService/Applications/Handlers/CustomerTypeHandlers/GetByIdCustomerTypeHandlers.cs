using AutoMapper;
using Common;
using CustomerService.DTOs.CustomerDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.CustomerTypeQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.CustomerTypeHandlers;

public class GetByIdCustomerTypeHandlers : IRequestHandler<GetByIdCustomerTypeQueries, ApiResponse<ReadCustomerTypeDTO>>
{
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  private readonly ILogger<GetByIdCustomerTypeHandlers> _logger;

  public GetByIdCustomerTypeHandlers(ApplicationDbContext dbContext, IMapper mapper, ILogger<GetByIdCustomerTypeHandlers> logger)
  {
    _dbContext = dbContext;
    _mapper = mapper;
    _logger = logger;
  }

  public async Task<ApiResponse<ReadCustomerTypeDTO>> Handle(GetByIdCustomerTypeQueries request, CancellationToken cancellationToken)
  {
    try
    {
      var customerType = await _dbContext.CustomerTypes.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
      if (customerType == null)
      {
        return new ApiResponse<ReadCustomerTypeDTO>(false, "No customer type found", null!);
      }
      var customerTypeDto = _mapper.Map<ReadCustomerTypeDTO>(customerType);
      _logger.LogInformation("Customer type retrieved successfully: {CustomerType}", customerTypeDto);
      return new ApiResponse<ReadCustomerTypeDTO>(true, "Customer type retrieved successfully", customerTypeDto);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error retrieving customer type: {Message}", ex.Message);
      return new ApiResponse<ReadCustomerTypeDTO>(false, ex.Message, null!);
    }
  }
}