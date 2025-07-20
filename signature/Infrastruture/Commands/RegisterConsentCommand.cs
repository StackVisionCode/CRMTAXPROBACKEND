using Application.DTOs;
using Application.Helpers;
using MediatR;

namespace signature.Infrastruture.Commands;

public record RegisterConsentCommand(RegisterConsentDto Payload) : IRequest<ApiResponse<bool>>;
