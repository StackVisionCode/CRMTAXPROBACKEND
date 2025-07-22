using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using SharedLibrary.Caching;

namespace SharedLibrary.Extensions;

public static class HealthCheckExtensions
{
    /// <summary>
    /// Agrega health checks para el sistema de caché híbrido
    /// </summary>
    public static IServiceCollection AddCacheHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck<HybridCacheHealthCheck>("hybrid-cache")
            .AddCheck<RedisHealthCheck>("redis-cache");

        return services;
    }
}

/// <summary>
/// Health check para el sistema de caché híbrido
/// </summary>
public class HybridCacheHealthCheck : IHealthCheck
{
    private readonly IHybridCache _hybridCache;
    private readonly ILogger<HybridCacheHealthCheck> _logger;

    public HybridCacheHealthCheck(IHybridCache hybridCache, ILogger<HybridCacheHealthCheck> logger)
    {
        _hybridCache = hybridCache;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var testKey = "health-check-test";
            var testValue = DateTime.UtcNow.Ticks;

            // Intentar operaciones básicas
            await _hybridCache.SetAsync(
                testKey,
                testValue,
                TimeSpan.FromSeconds(10),
                cancellationToken
            );
            var retrieved = await _hybridCache.GetAsync<long?>(testKey, cancellationToken);
            await _hybridCache.RemoveAsync(testKey, cancellationToken);

            var data = new Dictionary<string, object>
            {
                ["CacheMode"] = _hybridCache.CurrentCacheMode,
                ["RedisAvailable"] = _hybridCache.IsRedisAvailable,
            };

            if (retrieved == testValue)
            {
                return HealthCheckResult.Healthy(
                    $"Cache working properly in {_hybridCache.CurrentCacheMode} mode",
                    data
                );
            }
            else
            {
                return HealthCheckResult.Degraded(
                    $"Cache test failed in {_hybridCache.CurrentCacheMode} mode",
                    data: data
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache health check failed");
            return HealthCheckResult.Unhealthy("Cache health check failed", ex);
        }
    }
}

/// <summary>
/// Health check específico para Redis
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IHybridCache _hybridCache;

    public RedisHealthCheck(IHybridCache hybridCache)
    {
        _hybridCache = hybridCache;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        var data = new Dictionary<string, object>
        {
            ["RedisAvailable"] = _hybridCache.IsRedisAvailable,
            ["CurrentMode"] = _hybridCache.CurrentCacheMode,
        };

        if (_hybridCache.IsRedisAvailable)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Redis is available", data));
        }
        else
        {
            return Task.FromResult(
                HealthCheckResult.Degraded("Redis is not available, using local cache", data: data)
            );
        }
    }
}
