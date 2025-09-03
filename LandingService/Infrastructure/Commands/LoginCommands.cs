using Common;
using LandingService.Applications.DTO;
using MediatR;

namespace LandingService.Infrastructure.Commands;

public record class LoginCommands(LoginDTO Login):IRequest<ApiResponse<string>>;