using Common;
using LandingService.Applications.DTO;
using MediatR;

namespace LandingService.Infrastructure.Commands;

public record class CreateConfirmEmail(EmailConfirmDto request):IRequest<ApiResponse<string>>; 