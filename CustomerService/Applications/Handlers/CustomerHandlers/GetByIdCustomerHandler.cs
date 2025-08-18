using AutoMapper;
using Common;
using CustomerService.DTOs.CustomerDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.CustomerQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.CustomerHandlers;

public class GetByIdCustomerHanlder
    : IRequestHandler<GetByIdCustomerQueries, ApiResponse<ReadCustomerDTO>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetByIdCustomerHanlder> _logger;

    public GetByIdCustomerHanlder(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<GetByIdCustomerHanlder> logger
    )
    {
        _dbContext = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<ReadCustomerDTO>> Handle(
        GetByIdCustomerQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var customerDTO = await (
                from cust in _dbContext.Customers
                join ct in _dbContext.CustomerTypes on cust.CustomerTypeId equals ct.Id
                join occ in _dbContext.Occupations on cust.OccupationId equals occ.Id
                join ms in _dbContext.MaritalStatuses on cust.MaritalStatusId equals ms.Id
                join ci in _dbContext.ContactInfos on cust.Id equals ci.CustomerId into ciGroup
                from ci in ciGroup.DefaultIfEmpty()
                where cust.Id == request.Id
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
            ).FirstOrDefaultAsync(cancellationToken);

            if (customerDTO == null)
            {
                _logger.LogWarning("Customer with ID {Id} not found.", request.Id);
                return new ApiResponse<ReadCustomerDTO>(false, "Customer not found", null!);
            }

            return new ApiResponse<ReadCustomerDTO>(
                true,
                "Customer retrieved successfully",
                customerDTO
            );
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting customer: {Message}", e.Message);
            return new ApiResponse<ReadCustomerDTO>(false, e.Message, null!);
        }
    }
}
