using AutoMapper;
using Common;
using CustomerService.DTOs.AddressDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.AddressQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.AddressHandlers;

public class GetByIdAddressHandler
    : IRequestHandler<GetByIdAddressQueries, ApiResponse<ReadAddressDTO>>
{
    private readonly ILogger<GetByIdAddressHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetByIdAddressHandler(
        ILogger<GetByIdAddressHandler> logger,
        ApplicationDbContext dbContext,
        IMapper mapper
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<ReadAddressDTO>> Handle(
        GetByIdAddressQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await (
                from address in _dbContext.Addresses
                join customer in _dbContext.Customers on address.CustomerId equals customer.Id
                where address.Id == request.Id
                select new ReadAddressDTO
                {
                    Id = address.Id,
                    CustomerId = address.CustomerId,
                    Country = address.Country,
                    StreetAddress = address.StreetAddress,
                    ApartmentNumber = address.ApartmentNumber,
                    City = address.City,
                    State = address.State,
                    ZipCode = address.ZipCode,
                    Customer = customer.FirstName + " " + customer.LastName,
                    // Auditoría
                    CreatedAt = address.CreatedAt,
                    CreatedByTaxUserId = address.CreatedByTaxUserId,
                    UpdatedAt = address.UpdatedAt,
                    LastModifiedByTaxUserId = address.LastModifiedByTaxUserId,
                }
            ).FirstOrDefaultAsync(cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("Address with ID {Id} not found.", request.Id);
                return new ApiResponse<ReadAddressDTO>(false, "Address not found", null!);
            }

            return new ApiResponse<ReadAddressDTO>(true, "Address retrieved successfully", result);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error getting address: {Message}", ex.Message);
            return new ApiResponse<ReadAddressDTO>(false, ex.Message, null!);
        }
    }
}
