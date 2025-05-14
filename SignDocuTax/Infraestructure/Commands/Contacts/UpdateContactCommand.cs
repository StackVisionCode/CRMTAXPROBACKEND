using Common;
using DTOs.Contacts;
using MediatR;

namespace Commands.Contacts;

public record class UpdateContactCommand(UpdateContactDto UpdateContactDto) : IRequest<ApiResponse<bool>>;


