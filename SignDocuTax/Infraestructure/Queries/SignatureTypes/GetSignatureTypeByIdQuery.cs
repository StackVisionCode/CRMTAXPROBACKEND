using Common;
using Dtos.SignatureTypeDto;
using MediatR;

namespace Queries.SignatureTypes;

public record GetSignatureTypeByIdQuery(GetByIdSignatureTypeDto GetById) : IRequest<ApiResponse<SignatureTypeDto>>;
