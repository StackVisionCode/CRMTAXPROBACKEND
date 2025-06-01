using Common;
using CustomerService.DTOs.ContactInfoDTOs;
using MediatR;

namespace CustomerService.Commands.ContactInfoCommands;

public record class UpdateContactInfoCommands(UpdateContactInfoDTOs contactInfo)
    : IRequest<ApiResponse<bool>>;
