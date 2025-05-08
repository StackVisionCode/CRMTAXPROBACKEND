using Common;
using CustomerService.DTOs.CustomerDTOs;
using MediatR;

namespace CustomerService.Commands.CustomerTypeCommands;

public record class CreateCustomerTypeCommands(CustomerTypeDTO customerType) : IRequest<ApiResponse<bool>>;