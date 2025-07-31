using Common;
using DTOs.GeographyDTOs;
using MediatR;

namespace Queries.GeographyQueries;

public record class GetAllStatesQuery : IRequest<ApiResponse<List<StateDTO>>>;
