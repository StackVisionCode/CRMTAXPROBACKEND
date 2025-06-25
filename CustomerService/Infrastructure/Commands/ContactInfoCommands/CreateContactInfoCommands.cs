using Common;
using CustomerService.DTOs.ContactInfoDTOs;
using MediatR;

namespace CustomerService.Commands.ContactInfoCommands;

public record class CreateContactInfoCommands(CreateContactInfoDTOs contactInfo)
    : IRequest<ApiResponse<bool>>;
