using Common;
using CustomerService.DTOs.AddressDTOs;
using MediatR;

namespace CustomerService.Commands.AddressCommands;

public record class CreateAddressCommands(CreateAddressDTO addressDTO) : IRequest<ApiResponse<bool>>;