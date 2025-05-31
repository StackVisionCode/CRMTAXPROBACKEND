using Common;
using CustomerService.DTOs.CustomerDTOs;
using MediatR;

namespace CustomerService.Commands.CustomerCommands;

public record class UpdateCustomerCommands(UpdateCustomerDTO customer)
    : IRequest<ApiResponse<bool>>;
