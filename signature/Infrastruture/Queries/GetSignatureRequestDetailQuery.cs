using Application.DTOs.ReadDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastruture.Queries;

public record class GetSignatureRequestDetailQuery(Guid RequestId)
    : IRequest<ApiResponse<SignatureRequestDetailDto>>;
