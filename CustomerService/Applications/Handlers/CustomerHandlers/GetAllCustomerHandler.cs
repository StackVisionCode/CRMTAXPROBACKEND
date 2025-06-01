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
