using Common;
using CustomerService.DTOs.PreferredContactDTOs;
using MediatR;

namespace CustomerService.Commands.PreferredContactCommads;

public record class CreatePreferredContactCommands(CreatePreferredContactDTO preferredContact)
    : IRequest<ApiResponse<bool>>;
