using Common;
using CustomerService.DTOs.ContactInfoDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.ContactInfoQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.ContactInfoHandlers;

public class GetCustomerProfileHandler
    : IRequestHandler<GetCustomerProfileQuery, ApiResponse<CustomerProfileDTO>>
{
    private readonly ILogger<GetCustomerProfileHandler> _logger;
    private readonly ApplicationDbContext _dbContext;

    public GetCustomerProfileHandler(
        ILogger<GetCustomerProfileHandler> logger,
        ApplicationDbContext dbContext
    )
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    public async Task<ApiResponse<CustomerProfileDTO>> Handle(
        GetCustomerProfileQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var p = await (
                from cu in _dbContext.Customers
                join ci in _dbContext.ContactInfos on cu.Id equals ci.CustomerId
                where cu.Id == request.CustomerId
                select new CustomerProfileDTO
                {
                    PreparerId = cu.CompanyId, // Mapeo del ComapanyId a PreparerId
                    CustomerId = cu.Id,
                    FirstName = cu.FirstName,
                    LastName = cu.LastName,
                    MiddleName = cu.MiddleName,
                    Email = ci.Email,
                    PhoneNumber = ci.PhoneNumber,
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (p is null)
            {
                return new ApiResponse<CustomerProfileDTO>(false, "Customer not found");
            }

            return new ApiResponse<CustomerProfileDTO>(true, "Ok", p);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            return new ApiResponse<CustomerProfileDTO>(false, ex.Message);
        }
    }
}
