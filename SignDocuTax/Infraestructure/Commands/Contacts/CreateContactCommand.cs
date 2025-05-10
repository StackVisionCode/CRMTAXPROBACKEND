using Common;
using DTOs.Contacts;
using MediatR;

namespace Commands.Contacts;

public record class CreateContactCommand(CreateContactDto CreateContactDto) : IRequest<ApiResponse<bool>>;


