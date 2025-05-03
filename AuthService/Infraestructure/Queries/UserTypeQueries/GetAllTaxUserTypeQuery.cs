using Common;
using MediatR;
using UserDTOS;

namespace Queries.UserTypeQueries;

public record class GetAllTaxUserTypeQuery:IRequest<ApiResponse<List<TaxUserTypeDTO>>>;
