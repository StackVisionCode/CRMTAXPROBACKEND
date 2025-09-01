using LandingService.Applications.Services;

namespace LandingService.Infrastructure.Services;

public interface IGeolocationService
{
    Task<GeolocationInfo?> GetLocationInfoAsync(string ipAddress);
    Task<string> GetLocationDisplayAsync(string ipAddress);
}
