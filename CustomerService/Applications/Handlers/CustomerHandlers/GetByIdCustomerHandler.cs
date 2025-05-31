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
            ).FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);
            if (customerDTO == null)
            {
                _logger.LogWarning("Customers with ID {Id} not found.", request.Id);
                return new ApiResponse<ReadCustomerDTO>(false, "Customers not found", null!);
            }

            var dtos = _mapper.Map<ReadCustomerDTO>(customerDTO);
            return new ApiResponse<ReadCustomerDTO>(true, "Customer retrieved successfully", dtos);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error getting document: {Message}", e.Message);
            return new ApiResponse<ReadCustomerDTO>(false, e.Message, null!);
        }
    }
}
