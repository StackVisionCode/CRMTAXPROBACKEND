using AutoMapper;
using Common;
using CustomerService.DTOs.CustomerDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.CustomerQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.CustomerHandlers;

public class GetCustomersByCompanyHandler
    : IRequestHandler<GetCustomersByCompanyQueries, ApiResponse<List<ReadCustomerDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetCustomersByCompanyHandler> _logger;

    public GetCustomersByCompanyHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<GetCustomersByCompanyHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ReadCustomerDTO>>> Handle(
        GetCustomersByCompanyQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await (
                from cust in _dbContext.Customers
                join ct in _dbContext.CustomerTypes on cust.CustomerTypeId equals ct.Id
                join occ in _dbContext.Occupations on cust.OccupationId equals occ.Id
                join ms in _dbContext.MaritalStatuses on cust.MaritalStatusId equals ms.Id
                join ci in _dbContext.ContactInfos on cust.Id equals ci.CustomerId into ciGroup
                from ci in ciGroup.DefaultIfEmpty()
                where cust.CompanyId == request.CompanyId
                select new ReadCustomerDTO
                {
                    Id = cust.Id,
                    CompanyId = cust.CompanyId,
                    CustomerType = ct.Name,
                    CustomerTypeDescription = ct.Description,
                    FirstName = cust.FirstName,
                    LastName = cust.LastName,
                    MiddleName = cust.MiddleName,
                    DateOfBirth = cust.DateOfBirth,
                    SsnOrItin = cust.SsnOrItin,
                    IsActive = cust.IsActive,
                    IsLogin = ci != null && ci.IsLoggin,
                    Occupation = occ.Name,
                    MaritalStatus = ms.Name,
                    // Auditoría
                    CreatedAt = cust.CreatedAt,
                    CreatedByTaxUserId = cust.CreatedByTaxUserId,
                    UpdatedAt = cust.UpdatedAt,
                    LastModifiedByTaxUserId = cust.LastModifiedByTaxUserId,
                }
            ).ToListAsync(cancellationToken);

            if (result is null || !result.Any())
            {
                _logger.LogInformation(
                    "No customers found for Company: {CompanyId}",
                    request.CompanyId
                );
                return new ApiResponse<List<ReadCustomerDTO>>(
                    false,
                    "No customers found for this company",
                    new List<ReadCustomerDTO>()
                );
            }

            _logger.LogInformation(
                "Customers retrieved successfully for Company {CompanyId}: {Count} customers",
                request.CompanyId,
                result.Count
            );

            return new ApiResponse<List<ReadCustomerDTO>>(
                true,
                "Customers retrieved successfully",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving customers for Company {CompanyId}: {Message}",
                request.CompanyId,
                ex.Message
            );
            return new ApiResponse<List<ReadCustomerDTO>>(
                false,
                ex.Message,
                new List<ReadCustomerDTO>()
            );
        }
    }
}
