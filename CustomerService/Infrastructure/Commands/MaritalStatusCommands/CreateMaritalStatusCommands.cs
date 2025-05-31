using Common;
using CustomerService.DTOs.MaritalStatusDTOs;
using MediatR;

namespace CustomerService.Commands.MaritalStatusCommands;

public record class CreateMaritalStatusCommands(CreateMaritalStatusDTO maritalStatus)
    : IRequest<ApiResponse<bool>>;
