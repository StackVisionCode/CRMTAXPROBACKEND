using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace CommLinkService.Infrastructure.Security;

public interface IRateLimitingService
{
    Task<bool> IsAllowedAsync(string identifier, string action, int limit, TimeSpan window);
}

public sealed class RateLimitingService : IRateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingService> _logger;

    public RateLimitingService(IMemoryCache cache, ILogger<RateLimitingService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<bool> IsAllowedAsync(
        string identifier,
        string action,
        int limit,
        TimeSpan window
    )
    {
        var key = $"rate_limit:{identifier}:{action}";
        var currentCount = await _cache.GetOrCreateAsync(
            key,
            async entry =>
            {
                entry.SetAbsoluteExpiration(window);
                return 0;
            }
        );

        if (currentCount >= limit)
        {
            _logger.LogWarning(
                "Rate limit exceeded for {Identifier} on action {Action}",
                identifier,
                action
            );
            return false;
        }

        _cache.Set(key, currentCount + 1, window);
        return true;
    }
}
