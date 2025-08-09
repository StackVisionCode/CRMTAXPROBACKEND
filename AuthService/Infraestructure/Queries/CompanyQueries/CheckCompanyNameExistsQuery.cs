using Common;
using MediatR;

namespace Queries.CompanyQueries;

public record CheckCompanyNameExistsQuery(string CompanyName, Guid? ExcludeCompanyId = null)
    : IRequest<ApiResponse<bool>>;
