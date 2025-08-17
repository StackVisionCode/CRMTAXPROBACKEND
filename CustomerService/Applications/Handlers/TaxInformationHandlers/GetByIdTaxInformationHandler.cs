using AutoMapper;
using Common;
using CustomerService.DTOs.TaxInformationDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.TaxInformationQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.TaxInformationHandlers;

public class GetByIdTaxInformationHandler
    : IRequestHandler<GetByIdTaxInformationQueries, ApiResponse<ReadTaxInformationDTO>>
{
    private readonly ILogger<GetByIdTaxInformationHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetByIdTaxInformationHandler(
        ILogger<GetByIdTaxInformationHandler> logger,
        ApplicationDbContext dbContext,
        IMapper mapper
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ReadTaxInformationDTO>> Handle(
        GetByIdTaxInformationQueries request,
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
                where taxInformation.Id == request.Id
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
                    // NUEVOS: Auditoría
                    CreatedAt = taxInformation.CreatedAt,
                    CreatedByTaxUserId = taxInformation.CreatedByTaxUserId,
                    UpdatedAt = taxInformation.UpdatedAt,
                    LastModifiedByTaxUserId = taxInformation.LastModifiedByTaxUserId,
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("TaxInformation with ID {Id} not found.", request.Id);
                return new ApiResponse<ReadTaxInformationDTO>(
                    false,
                    "TaxInformation not found",
                    null!
                );
            }

            _logger.LogInformation(
                "TaxInformation retrieved successfully: {TaxInformationId}",
                result.Id
            );
            return new ApiResponse<ReadTaxInformationDTO>(
                true,
                "TaxInformation retrieved successfully",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving TaxInformation: {Message}", ex.Message);
            return new ApiResponse<ReadTaxInformationDTO>(false, ex.Message, null!);
        }
    }
}
