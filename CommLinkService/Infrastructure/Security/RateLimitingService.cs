using CommLinkService.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;

namespace CommLinkService.Infrastructure.Security;

public class RateLimitingService : IRateLimitingService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<RateLimitingService> _logger;

    public RateLimitingService(IMemoryCache cache, ILogger<RateLimitingService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public bool IsAllowed(string key, int maxRequests, TimeSpan timeWindow)
    {
        try
        {
            var cacheKey = $"rate_limit:{key}";
            var now = DateTime.UtcNow;

            if (!_cache.TryGetValue(cacheKey, out RateLimitEntry? entry) || entry == null)
            {
                entry = new RateLimitEntry { RequestCount = 1, WindowStart = now };

                _cache.Set(
                    cacheKey,
                    entry,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = timeWindow,
                        Priority = CacheItemPriority.Low,
                    }
                );

                return true;
            }

            if (now - entry.WindowStart > timeWindow)
            {
                entry.RequestCount = 1;
                entry.WindowStart = now;

                _cache.Set(
                    cacheKey,
                    entry,
                    new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = timeWindow,
                        Priority = CacheItemPriority.Low,
                    }
                );

                return true;
            }

            if (entry.RequestCount >= maxRequests)
            {
                _logger.LogWarning(
                    "Rate limit exceeded for key {Key}. Requests: {RequestCount}/{MaxRequests}",
                    key,
                    entry.RequestCount,
                    maxRequests
                );

                return false;
            }

            entry.RequestCount++;
            _cache.Set(
                cacheKey,
                entry,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = timeWindow - (now - entry.WindowStart),
                    Priority = CacheItemPriority.Low,
                }
            );

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking rate limit for key {Key}", key);
            return true; // Permitir en caso de error
        }
    }

    public void ClearUserRequests(string key)
    {
        try
        {
            _cache.Remove($"rate_limit:{key}");
            _logger.LogDebug("Cleared rate limit data for key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing rate limit for key {Key}", key);
        }
    }
}

internal sealed class RateLimitEntry
{
    public int RequestCount { get; set; }
    public DateTime WindowStart { get; set; }
}
