using Common;
using MediatR;
using UserDTOS;

namespace Commands.UserTypeCommands;

public record class DeleteTaxUserTypeCommands(TaxUserTypeDTO UserType) :IRequest<ApiResponse<bool>>;
