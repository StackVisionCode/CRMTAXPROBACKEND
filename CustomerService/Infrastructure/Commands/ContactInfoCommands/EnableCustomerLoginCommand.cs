using Common;
using CustomerService.DTOs.ContactInfoDTOs;
using MediatR;

namespace CustomerService.Commands.ContactInfoCommands;

public record class EnableCustomerLoginCommand(EnableLoginDTO Data) : IRequest<ApiResponse<bool>>;
