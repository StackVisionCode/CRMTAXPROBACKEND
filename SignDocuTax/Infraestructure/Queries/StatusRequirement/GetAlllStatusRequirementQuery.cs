using Common;
using MediatR;
using DTOs.StatusRequiremtDto; 

namespace Queries.StatusRequirement;

public record GetAlllStatusRequirementQuery : IRequest<ApiResponse<List<ReadRequiremenStatustDtos>>>;
