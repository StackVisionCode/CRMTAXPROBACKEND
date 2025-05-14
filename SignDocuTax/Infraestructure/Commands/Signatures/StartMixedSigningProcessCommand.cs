using Common;
using DTOs.Signatures;
using MediatR;

namespace Commands.Signatures;

public record StartMixedSigningProcessCommand(StartSigningProcessDto SigningProcessDto) 
    : IRequest<ApiResponse<SigningProcessResultDto>>;