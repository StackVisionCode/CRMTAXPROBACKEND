using AutoMapper;
using Common;
using CustomerService.DTOs.AddressDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.AddressQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.AddressHandlers;

public class GetAddressByCustomerIdHandler
    : IRequestHandler<GetAddressByCustomerIdQueries, ApiResponse<ReadAddressDTO>>
{
    private readonly ILogger<GetAddressByCustomerIdHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetAddressByCustomerIdHandler(
        ILogger<GetAddressByCustomerIdHandler> logger,
        ApplicationDbContext dbContext,
        IMapper mapper
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ReadAddressDTO>> Handle(
        GetAddressByCustomerIdQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await (
                from address in _dbContext.Addresses
                join customer in _dbContext.Customers on address.CustomerId equals customer.Id
                where address.CustomerId == request.CustomerId // Filter by CustomerId
                select new ReadAddressDTO
                {
                    Id = address.Id,
                    Country = address.Country,
                    StreetAddress = address.StreetAddress,
                    ApartmentNumber = address.ApartmentNumber,
                    City = address.City,
                    State = address.State,
                    ZipCode = address.ZipCode,
                    Customer = customer.FirstName + " " + customer.LastName,
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (result == null)
            {
                _logger.LogWarning(
                    "Address for Customer ID {CustomerId} not found.",
                    request.CustomerId
                );
                return new ApiResponse<ReadAddressDTO>(
                    false,
                    "Address not found for this customer",
                    null!
                );
            }

            return new ApiResponse<ReadAddressDTO>(true, "Address retrieved successfully", result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error getting Address for Customer ID {CustomerId}: {Message}",
                request.CustomerId,
                ex.Message
            );
            return new ApiResponse<ReadAddressDTO>(false, ex.Message, null!);
        }
    }
}
