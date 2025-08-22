using Common;
using DTOs.PublicDTOs;
using MediatR;

namespace Queries.PublicQueries;

public record GetCompanyPublicInfoQuery(Guid CompanyId)
    : IRequest<ApiResponse<CompanyPublicInfoDTO>>;
