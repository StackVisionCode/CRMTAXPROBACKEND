using AutoMapper;
using Common;
using CustomerService.DTOs.AddressDTOs;
using CustomerService.Infrastructure.Context;
using CustomerService.Queries.AddressQueries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.AddressHandlers;

public class GetAllAddressHandler
    : IRequestHandler<GetAllAddressQueries, ApiResponse<List<ReadAddressDTO>>>
{
    private readonly ILogger<GetAllAddressHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public GetAllAddressHandler(
        ILogger<GetAllAddressHandler> logger,
        ApplicationDbContext dbContext,
        IMapper mapper
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<List<ReadAddressDTO>>> Handle(
        GetAllAddressQueries request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var result = await (
                from address in _dbContext.Addresses
                join customer in _dbContext.Customers on address.CustomerId equals customer.Id
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
            ).ToListAsync(cancellationToken);

            if (result is null || !result.Any())
            {
                _logger.LogInformation("No addresses found.");
                return new ApiResponse<List<ReadAddressDTO>>(
                    false,
                    "No addresses found",
                    new List<ReadAddressDTO>()
                );
            }

            _logger.LogInformation(
                "Addresses retrieved successfully: {Count} records",
                result.Count
            );
            return new ApiResponse<List<ReadAddressDTO>>(
                true,
                "Addresses retrieved successfully",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error retrieving addresses: {Message}", ex.Message);
            return new ApiResponse<List<ReadAddressDTO>>(
                false,
                ex.Message,
                new List<ReadAddressDTO>()
            );
        }
    }
}
