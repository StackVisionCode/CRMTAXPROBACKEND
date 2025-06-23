using AuthService.DTOs.SessionDTOs;
using Common;
using MediatR;

namespace Commands.CustomerCommands;

public record CustomerLoginCommand(
    CustomerLoginRequestDTO Petition,
    string IpAddress,
    string Device
) : IRequest<ApiResponse<LoginResponseDTO>>;
