using Common;
using DTOs.SignatureEventTypeDto;
using MediatR;

namespace Commands.SignatureEventType
{
    public record UpdateSignatureEventTypeCommand(UpdateSignatureEventTypeDto SignatureEventType) 
        : IRequest<ApiResponse<bool>>;
}
