using Common;
using Dtos.SignatureTypeDto;
using MediatR;

namespace Queries.SignatureTypes;

public record GetAllSignatureTypeQuery : IRequest<ApiResponse<IEnumerable<SignatureTypeDto>>>;
