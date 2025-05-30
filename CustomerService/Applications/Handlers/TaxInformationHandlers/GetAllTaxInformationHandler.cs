using AutoMapper;
using Common;
using CustomerService.DTOs.TaxInformationDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.TaxInformationQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.TaxInformationHandlers;

public class GetAllTaxInformationHandler : IRequestHandler<GetAllTaxInformationQueries, ApiResponse<List<ReadTaxInformationDTO>>>
{
  private readonly ILogger<GetAllTaxInformationHandler> _logger;
  private readonly ApplicationDbContext _dbContext;
  private readonly IMapper _mapper;
  public GetAllTaxInformationHandler(ILogger<GetAllTaxInformationHandler> logger, ApplicationDbContext dbContext, IMapper mapper)
  {
    _logger = logger;
    _dbContext = dbContext;
    _mapper = mapper;
  }
  public async Task<ApiResponse<List<ReadTaxInformationDTO>>> Handle(GetAllTaxInformationQueries request, CancellationToken cancellationToken)
  {
    try
    {
      var result = await (
        from taxInformation in _dbContext.TaxInformations
        join customer in _dbContext.Customers on taxInformation.CustomerId equals customer.Id
        join filingStatus in _dbContext.FilingStatuses on taxInformation.FilingStatusId equals filingStatus.Id

        select new ReadTaxInformationDTO
        {
          Id = taxInformation.Id,
          Customer = customer.FirstName + " " + customer.LastName,
          FilingStatus = filingStatus.Name,
          LastYearAGI = taxInformation.LastYearAGI,
          BankAccountNumber = taxInformation.BankAccountNumber,
          BankRoutingNumber = taxInformation.BankRoutingNumber,
          IsReturningCustomer = taxInformation.IsReturningCustomer,
        }
      ).ToListAsync(cancellationToken);

      if (result is null || !result.Any())
      {
        _logger.LogInformation("No tax information found.");
        return new ApiResponse<List<ReadTaxInformationDTO>>(false, "No tax information found", null!);
      }

      var taxInformationDTO = _mapper.Map<List<ReadTaxInformationDTO>>(result);
      _logger.LogInformation("Tax information retrieved successfully: {TaxInformation}", taxInformationDTO);
      return new ApiResponse<List<ReadTaxInformationDTO>>(true, "Tax information retrieved successfully", result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred while retrieving tax information.");
      return new ApiResponse<List<ReadTaxInformationDTO>>(false, ex.Message, null!);
    }
  }
}