using AuthService.DTOs.SessionDTOs;
using Common;
using MediatR;

namespace Commands.SessionCommands;

public record class LoginCommands(LoginRequestDTO Petition, string IpAddress, string Device)
    : IRequest<ApiResponse<LoginResponseDTO>>;
