using Common;
using CustomerService.DTOs.CustomerDTOs;
using MediatR;

namespace CustomerService.Commands.CustomerCommands;

public record class CreateCustomerCommands(CustomerDTO customer) : IRequest<ApiResponse<bool>>;