using Common;
using CustomerService.DTOs.AddressDTOs;
using MediatR;

namespace CustomerService.DTOs.AddressCommands;

public record class UpdateAddressCommands(UpdateAddressDTO address) : IRequest<ApiResponse<bool>>;
