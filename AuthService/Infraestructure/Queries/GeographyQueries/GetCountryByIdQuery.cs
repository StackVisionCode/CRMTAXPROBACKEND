using Common;
using DTOs.GeographyDTOs;
using MediatR;

namespace Queries.GeographyQueries;

public record class GetCountryByIdQuery(int CountryId) : IRequest<ApiResponse<CountryDTO>>;
