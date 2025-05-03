using Common;
using MediatR;
using UserDTOS;

namespace Commands.UserTypeCommands
{
    public record class CreateTaxUserTypeCommands(TaxUserTypeDTO  Typeuser) : IRequest<ApiResponse<bool>>;
}