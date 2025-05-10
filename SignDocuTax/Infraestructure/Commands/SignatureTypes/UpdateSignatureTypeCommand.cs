using Common;
using Dtos.SignatureTypeDto;
using MediatR;


namespace Commands.SignatureTypes;

public record UpdateSignatureTypeCommand(UpdateSignatureTypeDto SignatureType) : IRequest<ApiResponse<bool>>;
