using AuthService.Applications.Services;

namespace AuthService.Infraestructure.Services;

public interface IGeolocationService
{
    Task<GeolocationInfo?> GetLocationInfoAsync(string ipAddress);
    Task<string> GetLocationDisplayAsync(string ipAddress);
}
