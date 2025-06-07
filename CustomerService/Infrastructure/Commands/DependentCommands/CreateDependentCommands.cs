using Common;
using CustomerService.DTOs.DependentDTOs;
using MediatR;

namespace CustomerService.Commands.DependentCommands;

public record class CreateDependentCommands(CreateDependentDTO dependent)
    : IRequest<ApiResponse<bool>>;
