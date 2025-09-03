using Common;
using LandingService.Applications.DTO;
using MediatR;

namespace LandingService.Infrastructure.Commands;

public record class CreateRegisterCommands(RegisterDTO requestDto):IRequest<ApiResponse<bool>>;