using Common;
using DTOs.StatusRequiremtDto;  
using MediatR;

namespace Commands.StatusRequirement;

public record CreateStatusRequirementCommand(CreateStatusRequirementDto StatusRequirement) : IRequest<ApiResponse<bool>>;
