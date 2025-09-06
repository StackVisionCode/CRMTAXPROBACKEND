using CommLinkService.Application.DTOs.VideoCallDTOs;

namespace CommLinkService.Infrastructure.Services;

public interface IWebRTCService
{
    Task<TurnCredentialsDto> GenerateTurnCredentialsAsync();
    Task<bool> ValidateTurnServerHealthAsync();
}
