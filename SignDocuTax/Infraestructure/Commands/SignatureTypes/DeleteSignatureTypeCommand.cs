using Common;
using Dtos.SignatureTypeDto;
using MediatR;

namespace Commands.SignatureTypes;

public record DeleteSignatureTypeCommand(DeleteSignatureTypeDto Id) : IRequest<ApiResponse<bool>>;
