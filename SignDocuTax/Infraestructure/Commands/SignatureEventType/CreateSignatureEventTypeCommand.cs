using Common;
using DTOs.SignatureEventTypeDto;
using MediatR;

namespace Commands.SignatureEventType
{
    public record CreateSignatureEventTypeCommand(CreateSignatureEventTypeDto SignatureEventType) 
        : IRequest<ApiResponse<bool>>;
}
