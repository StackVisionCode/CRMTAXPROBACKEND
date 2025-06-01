using Common;
using CustomerService.DTOs.DependentDTOs;
using MediatR;

namespace CustomerService.Commands.DependentCommands;

public record class UpdateDependentCommands(UpdateDependentDTO dependent)
    : IRequest<ApiResponse<bool>>;
