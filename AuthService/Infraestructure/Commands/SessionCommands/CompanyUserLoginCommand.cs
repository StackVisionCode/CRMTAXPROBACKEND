using AuthService.DTOs.CompanyUserSessionDTOs;
using AuthService.DTOs.SessionDTOs;
using Common;
using MediatR;

namespace Commands.SessionCommands;

public record class CompanyUserLoginCommand(
    CompanyUserLoginRequestDTO Petition,
    string IpAddress,
    string Device
) : IRequest<ApiResponse<LoginResponseDTO>>;
