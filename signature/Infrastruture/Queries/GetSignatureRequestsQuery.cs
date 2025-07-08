using Application.DTOs.ReadDTOs;
using Application.Helpers;
using MediatR;

namespace Infrastruture.Queries;

public record class GetSignatureRequestsQuery()
    : IRequest<ApiResponse<List<SignatureRequestSummaryDto>>>;
