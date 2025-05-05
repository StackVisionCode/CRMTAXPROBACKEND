using AuthService.DTOs.SessionDTOs;
using Common;
using MediatR;

namespace Commands.SessionCommands;

public record class LoginCommands(string Email, string Password, string IpAddress, string Device, bool RememberMe ) : IRequest<ApiResponse<LoginResponseDTO>>;