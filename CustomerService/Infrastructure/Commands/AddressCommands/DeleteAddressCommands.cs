using Common;
using MediatR;

namespace CustomerService.DTOs.AddressCommands;

public record class DeleteAddressCommands(Guid Id) : IRequest<ApiResponse<bool>>;
