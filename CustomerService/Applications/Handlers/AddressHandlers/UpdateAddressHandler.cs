using AutoMapper;
using Common;
using CustomerService.DTOs.AddressCommands;
using CustomerService.DTOs.AddressDTOs;
using CustomerService.Infrastructure.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace CustomerService.Hanlders.AddressHandlers;

public class UpdateAddressHandler : IRequestHandler<UpdateAddressCommands, ApiResponse<bool>>
{
    private readonly ILogger<UpdateAddressHandler> _logger;
    private readonly ApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateAddressHandler(
        ILogger<UpdateAddressHandler> logger,
        ApplicationDbContext context,
        IMapper mapper
    )
    {
        _logger = logger;
        _context = context;
        _mapper = mapper;
    }

    public async Task<ApiResponse<bool>> Handle(
        UpdateAddressCommands request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var existingAddress = await _context.Addresses.FirstOrDefaultAsync(
                c => c.Id == request.address.Id,
                cancellationToken
            );

            if (existingAddress == null)
            {
                _logger.LogWarning("Address with ID {Id} not found for update", request.address.Id);
                return new ApiResponse<bool>(false, "Address not found", false);
            }

            var duplicateExists = await _context.Addresses.AnyAsync(
                c => c.StreetAddress == request.address.StreetAddress && c.Id != request.address.Id,
                cancellationToken
            );

            if (duplicateExists)
            {
                _logger.LogWarning(
                    "Address with StreetAddress {StreetAddress} already exists",
                    request.address.StreetAddress
                );
                return new ApiResponse<bool>(
                    false,
                    "Address with this StreetAddress already exists.",
                    false
                );
            }
            _mapper.Map(request.address, existingAddress);
            existingAddress.UpdatedAt = DateTime.UtcNow;

            _context.Addresses.Update(existingAddress);
            var result = await _context.SaveChangesAsync(cancellationToken) > 0;
            if (result)
            {
                _logger.LogInformation(
                    "Address with ID {Id} updated successfully",
                    existingAddress.Id
                );
                return new ApiResponse<bool>(true, "Address updated successfully", true);
            }
            else
            {
                _logger.LogError("Failed to update address with ID {Id}", existingAddress.Id);
                return new ApiResponse<bool>(false, "Failed to update address", false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "An error occurred while updating address with ID {Id}",
                request.address.Id
            );
            return new ApiResponse<bool>(
                false,
                "An error occurred while updating the address",
                false
            );
        }
    }
}
