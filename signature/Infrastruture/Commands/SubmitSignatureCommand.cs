using Application.Helpers;
using MediatR;
using signature.Application.DTOs;

namespace signature.Infrastruture.Commands;

public record SubmitSignatureCommand(SignDocumentDto Payload) : IRequest<ApiResponse<bool>>;
