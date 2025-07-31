using Common;
using DTOs.GeographyDTOs;
using MediatR;

namespace Queries.GeographyQueries;

public record class GetStateByIdQuery(int StateId) : IRequest<ApiResponse<StateDTO>>;
