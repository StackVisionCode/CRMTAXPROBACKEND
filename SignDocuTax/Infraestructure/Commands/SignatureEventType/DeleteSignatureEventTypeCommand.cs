using Common;
using DTOs.SignatureEventTypeDto;
using MediatR;

namespace Commands.SignatureEventType
{
    public record DeleteSignatureEventTypeCommand(DeleteSignatureEventTypeDto SignatureEventType) 
        : IRequest<ApiResponse<bool>>;
}
