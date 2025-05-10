using Common;
using DTOs.StatusRequiremtDto;  
using MediatR;

namespace Commands.StatusRequirement;

public record UpdateStatusRequirementCommand(UpdateStatusRequirementDto StatusRequirement) : IRequest<ApiResponse<bool>>;
