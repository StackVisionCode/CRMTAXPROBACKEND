using AutoMapper;
using Common;
using CustomerService.DTOs.TaxInformationDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.TaxInformationQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.TaxInformationHandlers;

public class GetAllTaxInformationHandler
    : IRequestHandler<GetAllTaxInformationQueries, ApiResponse<List<ReadTaxInformationDTO>>>
{
    private readonly ILogger<GetAllTaxInformationHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetAllTaxInformationHandler(
        ILogger<GetAllTaxInformationHandler> logger,
        ApplicationDbContext dbContext,
        IMapper mapper
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<ReadTaxInformationDTO>>> Handle(
        GetAllTaxInformationQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await (
                from taxInformation in _dbContext.TaxInformations
                join customer in _dbContext.Customers
                    on taxInformation.CustomerId equals customer.Id
                join filingStatus in _dbContext.FilingStatuses
                    on taxInformation.FilingStatusId equals filingStatus.Id
                select new ReadTaxInformationDTO
                {
                    Id = taxInformation.Id,
                    CustomerId = taxInformation.CustomerId,
                    FilingStatusId = taxInformation.FilingStatusId,
                    Customer = customer.FirstName + " " + customer.LastName,
                    FilingStatus = filingStatus.Name,
                    LastYearAGI = taxInformation.LastYearAGI,
                    BankAccountNumber = taxInformation.BankAccountNumber,
                    BankRoutingNumber = taxInformation.BankRoutingNumber,
                    IsReturningCustomer = taxInformation.IsReturningCustomer,
                    // Auditoría
                    CreatedAt = taxInformation.CreatedAt,
                    CreatedByTaxUserId = taxInformation.CreatedByTaxUserId,
                    UpdatedAt = taxInformation.UpdatedAt,
                    LastModifiedByTaxUserId = taxInformation.LastModifiedByTaxUserId,
                }
            ).ToListAsync(cancellationToken);

            if (result is null || !result.Any())
            {
                _logger.LogInformation("No tax information found.");
                return new ApiResponse<List<ReadTaxInformationDTO>>(
                    false,
                    "No tax information found",
                    new List<ReadTaxInformationDTO>()
                );
            }

            _logger.LogInformation(
                "Tax information retrieved successfully: {Count} records",
                result.Count
            );
            return new ApiResponse<List<ReadTaxInformationDTO>>(
                true,
                "Tax information retrieved successfully",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving tax information.");
            return new ApiResponse<List<ReadTaxInformationDTO>>(
                false,
                ex.Message,
                new List<ReadTaxInformationDTO>()
            );
        }
    }
}
