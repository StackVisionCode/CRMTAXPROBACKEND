using Common;
using DTOs.StatusRequiremtDto;  
using MediatR;

namespace Commands.StatusRequirement;

public record DeleteStatusRequirementCommand(DeleteStatusRequirementDto StatusRequirement) : IRequest<ApiResponse<bool>>;
