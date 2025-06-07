using Common;
using MediatR;

namespace CustomerService.Commands.ContactInfoCommands;

public record class DeleteContactInfoCommands(Guid Id) : IRequest<ApiResponse<bool>>;
