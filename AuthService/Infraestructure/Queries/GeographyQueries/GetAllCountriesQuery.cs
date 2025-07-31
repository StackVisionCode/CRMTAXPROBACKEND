using Common;
using DTOs.GeographyDTOs;
using MediatR;

namespace Queries.GeographyQueries;

public record class GetAllCountriesQuery : IRequest<ApiResponse<List<CountryDTO>>>;
