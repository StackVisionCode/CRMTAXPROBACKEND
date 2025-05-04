using Common;
using MediatR;
using UserDTOS;

namespace Queries.UserTypeQueries;
public record class GetTaxUserByIdQuery(int UsertTaxTypeId) : IRequest<ApiResponse<TaxUserTypeDTO>>;
