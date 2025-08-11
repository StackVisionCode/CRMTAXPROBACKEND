using AuthService.DTOs.CompanyDTOs;
using Common;
using MediatR;

namespace Commands.UserCommands;

public record UpdateCompanyPlanCommand(UpdateCompanyPlanDTO CompanyPlanData)
    : IRequest<ApiResponse<CompanyPlanUpdateResultDTO>>;
