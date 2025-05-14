using Common;
using DTOs.Signers;
using MediatR;

namespace Commands.Signers
{
    public record class CreateExternalSignerCommand(CreateExternalSignerDto CreateExternalSignerDto) : IRequest<ApiResponse<bool>>;
}
