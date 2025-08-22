namespace CommLinkService.Infrastructure.Services;

public interface IRateLimitingService
{
    bool IsAllowed(string key, int maxRequests, TimeSpan timeWindow);
    void ClearUserRequests(string key);
}
