using AutoMapper;
using Common;
using CustomerService.DTOs.CustomerDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.CustomerQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.CustomerHandlers;

public class GetAllCustomerHandler
    : IRequestHandler<GetAllCustomerQueries, ApiResponse<List<ReadCustomerDTO>>>
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly ILogger<GetAllCustomerHandler> _logger;

    public GetAllCustomerHandler(
        ApplicationDbContext dbContext,
        IMapper mapper,
        ILogger<GetAllCustomerHandler> logger
    )
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ReadCustomerDTO>>> Handle(
        GetAllCustomerQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await (
                from customer in _dbContext.Customers
                join customerType in _dbContext.CustomerTypes
                    on customer.CustomerTypeId equals customerType.Id
                join occupation in _dbContext.Occupations
                    on customer.OccupationId equals occupation.Id
                join maritalStatus in _dbContext.MaritalStatuses
                    on customer.MaritalStatusId equals maritalStatus.Id
                join contatInfo in _dbContext.ContactInfos
                    on customer.Id equals contatInfo.CustomerId

                select new ReadCustomerDTO
                {
                    Id = customer.Id,
                    CustomerType = customerType.Name,
                    CustomerTypeDescription = customerType.Description,
                    FirstName = customer.FirstName,
                    LastName = customer.LastName,
                    MiddleName = customer.MiddleName,
                    DateOfBirth = customer.DateOfBirth,
                    SsnOrItin = customer.SsnOrItin,
                    IsActive = customer.IsActive,
                    IsLogin = contatInfo.IsLoggin,
                    Occupation = occupation.Name,
                    MaritalStatus = maritalStatus.Name,
                }
            ).ToListAsync();
            if (result is null || !result.Any())
            {
                _logger.LogInformation("No customers found.");
                return new ApiResponse<List<ReadCustomerDTO>>(false, "No customers found", null!);
            }

            var customerDtos = _mapper.Map<List<ReadCustomerDTO>>(result);
            _logger.LogInformation("Customers retrieved successfully: {Customers}", customerDtos);
            return new ApiResponse<List<ReadCustomerDTO>>(
                true,
                "Customers retrieved successfully",
                customerDtos
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving customers: {Message}", ex.Message);
            return new ApiResponse<List<ReadCustomerDTO>>(false, ex.Message, null!);
        }
    }
}
