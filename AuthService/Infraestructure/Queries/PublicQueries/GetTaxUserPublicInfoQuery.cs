using Common;
using DTOs.PublicDTOs;
using MediatR;

namespace Queries.PublicQueries;

public record GetTaxUserPublicInfoQuery(Guid TaxUserId)
    : IRequest<ApiResponse<TaxUserPublicInfoDTO>>;
