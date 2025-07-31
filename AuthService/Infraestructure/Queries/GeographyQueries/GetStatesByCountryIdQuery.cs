using Common;
using DTOs.GeographyDTOs;
using MediatR;

namespace Queries.GeographyQueries;

public record class GetStatesByCountryIdQuery(int CountryId)
    : IRequest<ApiResponse<List<StateDTO>>>;
