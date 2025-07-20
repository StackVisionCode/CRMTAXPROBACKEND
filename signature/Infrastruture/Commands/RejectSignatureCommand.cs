using Application.Helpers;
using MediatR;
using signature.Application.DTOs;

namespace signature.Infrastruture.Commands;

public record RejectSignatureCommand(RejectSignatureDto Payload)
    : IRequest<ApiResponse<RejectResultDto>>;
