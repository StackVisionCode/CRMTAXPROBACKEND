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
                join ci in _dbContext.ContactInfos on cust.Id equals ci.CustomerId into ciGroup // ← agrupo
                from ci in ciGroup.DefaultIfEmpty() // ← LEFT JOIN
                select new ReadCustomerDTO
                {
                    Id = cust.Id,
                    CustomerType = ct.Name,
                    CustomerTypeDescription = ct.Description,
                    FirstName = cust.FirstName,
                    LastName = cust.LastName,
                    MiddleName = cust.MiddleName,
                    DateOfBirth = cust.DateOfBirth,
                    SsnOrItin = cust.SsnOrItin,
                    IsActive = cust.IsActive,
                    IsLogin = ci != null && ci.IsLoggin, // si no hay contacto → false
                    Occupation = occ.Name,
                    MaritalStatus = ms.Name,
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
