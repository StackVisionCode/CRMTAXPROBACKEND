using Common;
using DTOs.Signatures;
using MediatR;

namespace Commands.Signatures;

public record CreateSignatureEventCommand(CreateSignatureEventDto SignatureEventDto) 
    : IRequest<ApiResponse<SignatureEventResultDto>>;