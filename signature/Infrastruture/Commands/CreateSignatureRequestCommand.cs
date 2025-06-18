using Application.Helpers;
using MediatR;
using signature.Application.DTOs;

namespace signature.Infrastruture.Commands;

public record CreateSignatureRequestCommand(CreateSignatureRequestDto Payload)
    : IRequest<ApiResponse<bool>>;
