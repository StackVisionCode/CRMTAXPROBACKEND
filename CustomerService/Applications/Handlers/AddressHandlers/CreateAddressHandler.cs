using AutoMapper;
using Common;
using CustomerService.Commands.AddressCommands;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Handlers.AddressHandlers;

public class CreateAddressHandler : IRequestHandler<CreateAddressCommands, ApiResponse<bool>>
{
    private readonly ILogger<CreateAddressHandler> _logger;
    private readonly ApplicationDbContext _dbContext;
    private readonly IMapper _mapper;

    public CreateAddressHandler(
        ILogger<CreateAddressHandler> logger,
        ApplicationDbContext dbContext,
        IMapper mapper
    )
    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<ApiResponse<bool>> Handle(
        CreateAddressCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var exists = await _dbContext.Addresses.AnyAsync(
                c => c.StreetAddress == request.addressDTO.StreetAddress,
                cancellationToken
            );

            if (exists)
            {
                _logger.LogWarning(
                    "Address already exists with StreetAddress: {StreetAddress}",
                    request.addressDTO.StreetAddress
                );
                return new ApiResponse<bool>(
                    false,
                    "Address with this StreetAddress already exists.",
                    false
                );
            }
            var address = _mapper.Map<Domains.Customers.Address>(request.addressDTO);
            address.CreatedAt = DateTime.UtcNow;
            await _dbContext.Addresses.AddAsync(address, cancellationToken);
            var result = await _dbContext.SaveChangesAsync(cancellationToken) > 0;
            _logger.LogInformation("Address created successfully: {Address}", address);
            return new ApiResponse<bool>(
                result,
                result ? "Address created successfully" : "Failed to create address",
                result
            );
        }
        catch (Exception ex)
        {
            _logger.LogError("Error creating address: {Message}", ex.Message);
            return new ApiResponse<bool>(false, ex.Message, false);
        }
    }
}
