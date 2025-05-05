using Common;
using Dtos.SignatureTypeDto;
using MediatR;

namespace Commands.SignatureTypes;

public record CreateSignatureTypeCommand(CreateSignatureTypeDto SignatureType) : IRequest<ApiResponse<bool>>;
