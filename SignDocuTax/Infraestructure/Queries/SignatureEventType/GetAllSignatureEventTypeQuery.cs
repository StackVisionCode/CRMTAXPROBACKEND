using Common;
using DTOs.SignatureEventTypeDto;
using MediatR;

namespace Queries.SignatureEventType
{
    public record GetAllSignatureEventTypeQuery() : IRequest<ApiResponse<List<ReadSignatureEventTypeDto>>>;
}
