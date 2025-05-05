using Common;
using MediatR;
using UserDTOS;

namespace Commands.UserTypeCommands;

public record class DeleteTaxUserTypeCommands(int UserTypeId) :IRequest<ApiResponse<bool>>;
