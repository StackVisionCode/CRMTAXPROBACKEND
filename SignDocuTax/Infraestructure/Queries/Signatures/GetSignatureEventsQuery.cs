using Common;
using DTOs.Signatures;
using MediatR;

namespace Queries.Signatures;

public record GetSignatureEventsQuery(int DocumentId) 
    : IRequest<ApiResponse<List<SignatureEventDetailDto>>>;