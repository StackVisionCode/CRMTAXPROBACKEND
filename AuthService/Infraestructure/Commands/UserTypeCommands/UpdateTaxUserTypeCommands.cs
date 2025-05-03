using Common;
using MediatR;
using UserDTOS;

namespace Commands.UserTypeCommands;

public record class UpdateTaxUserTypeCommands(TaxUserTypeDTO UserType) : IRequest<ApiResponse<bool>>;
