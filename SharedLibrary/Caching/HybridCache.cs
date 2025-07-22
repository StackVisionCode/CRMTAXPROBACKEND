using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SharedLibrary.Caching;

/// <summary>
/// Implementación del sistema de caché híbrido con contingencia automática
/// </summary>
public sealed class HybridCache : IHybridCache, IDisposable
{
    private readonly IDistributedCache? _distributedCache;
    private readonly IMemoryCache _memoryCache;
    private readonly HybridCacheOptions _options;
    private readonly ILogger<HybridCache> _logger;
    private readonly Timer _healthCheckTimer;
    private readonly SemaphoreSlim _healthCheckSemaphore;

    private volatile bool _isRedisAvailable = true;
    private int _consecutiveFailures = 0;
    private DateTime _lastFailureTime = DateTime.MinValue;
    private bool _disposed = false;

    public HybridCache(
        IDistributedCache? distributedCache,
        IMemoryCache memoryCache,
        HybridCacheOptions options,
        ILogger<HybridCache> logger
    )
    {
        _distributedCache = distributedCache;
        _memoryCache = memoryCache;
        _options = options;
        _logger = logger;
        _healthCheckSemaphore = new SemaphoreSlim(1, 1);

        // Inicializar el timer de verificación de salud
        _healthCheckTimer = new Timer(
            PerformHealthCheck,
            null,
            _options.HealthCheck.CheckInterval,
            _options.HealthCheck.CheckInterval
        );

        _logger.LogInformation(
            "HybridCache initialized - Redis: {RedisEnabled}, Local: Enabled",
            _distributedCache != null ? "Enabled" : "Disabled"
        );
    }

    public bool IsRedisAvailable => _isRedisAvailable && _distributedCache != null;

    public string CurrentCacheMode => IsRedisAvailable ? "Redis" : "Local";

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var prefixedKey = GetPrefixedKey(key);

        // Intentar primero con Redis si está disponible
        if (IsRedisAvailable)
        {
            try
            {
                var redisValue = await _distributedCache!.GetStringAsync(
                    prefixedKey,
                    cancellationToken
                );
                if (redisValue != null)
                {
                    var result = JsonSerializer.Deserialize<T>(redisValue);
                    _logger.LogDebug("Cache hit in Redis for key: {Key}", key);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis get operation failed for key: {Key}. Falling back to local cache",
                    key
                );
                await HandleRedisFailure();
            }
        }

        // Fallback a caché local
        if (_memoryCache.TryGetValue(prefixedKey, out T? localValue))
        {
            _logger.LogDebug("Cache hit in local memory for key: {Key}", key);
            return localValue;
        }

        _logger.LogDebug("Cache miss for key: {Key}", key);
        return default;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default
    )
    {
        var prefixedKey = GetPrefixedKey(key);
        var effectiveExpiry = expiry ?? _options.Redis.DefaultExpiry;
        var localExpiry = expiry ?? _options.Local.DefaultExpiry;

        // Intentar guardar en Redis primero si está disponible
        if (IsRedisAvailable)
        {
            try
            {
                var serializedValue = JsonSerializer.Serialize(value);
                var distributedCacheOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = effectiveExpiry,
                };

                await _distributedCache!.SetStringAsync(
                    prefixedKey,
                    serializedValue,
                    distributedCacheOptions,
                    cancellationToken
                );

                _logger.LogDebug(
                    "Value cached in Redis for key: {Key}, expiry: {Expiry}",
                    key,
                    effectiveExpiry
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis set operation failed for key: {Key}. Saving only to local cache",
                    key
                );
                await HandleRedisFailure();
            }
        }

        // Siempre guardar también en caché local como respaldo
        var memoryCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = localExpiry,
            Size = 1, // Para control de tamaño
        };

        _memoryCache.Set(prefixedKey, value, memoryCacheOptions);
        _logger.LogDebug(
            "Value cached in local memory for key: {Key}, expiry: {Expiry}",
            key,
            localExpiry
        );
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var prefixedKey = GetPrefixedKey(key);

        // Remover de Redis si está disponible
        if (IsRedisAvailable)
        {
            try
            {
                await _distributedCache!.RemoveAsync(prefixedKey, cancellationToken);
                _logger.LogDebug("Key removed from Redis: {Key}", key);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis remove operation failed for key: {Key}", key);
                await HandleRedisFailure();
            }
        }

        // Siempre remover del caché local
        _memoryCache.Remove(prefixedKey);
        _logger.LogDebug("Key removed from local memory: {Key}", key);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var prefixedKey = GetPrefixedKey(key);

        // Verificar en Redis primero si está disponible
        if (IsRedisAvailable)
        {
            try
            {
                var redisValue = await _distributedCache!.GetStringAsync(
                    prefixedKey,
                    cancellationToken
                );
                if (redisValue != null)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Redis exists check failed for key: {Key}. Checking local cache",
                    key
                );
                await HandleRedisFailure();
            }
        }

        // Verificar en caché local
        return _memoryCache.TryGetValue(prefixedKey, out _);
    }

    private string GetPrefixedKey(string key)
    {
        return $"{_options.Redis.KeyPrefix}:{key}";
    }

    private async Task HandleRedisFailure()
    {
        Interlocked.Increment(ref _consecutiveFailures);
        _lastFailureTime = DateTime.UtcNow;

        if (_consecutiveFailures >= _options.HealthCheck.FailureThreshold && _isRedisAvailable)
        {
            _isRedisAvailable = false;
            _logger.LogError(
                "Redis marked as unavailable after {FailureCount} consecutive failures. Switching to local cache only.",
                _consecutiveFailures
            );
        }
    }

    private async void PerformHealthCheck(object? state)
    {
        if (_disposed || _distributedCache == null)
            return;

        if (!await _healthCheckSemaphore.WaitAsync(100))
            return;

        try
        {
            // Si Redis está marcado como disponible, hacer un check ligero
            if (_isRedisAvailable)
            {
                await QuickHealthCheck();
            }
            // Si Redis está marcado como no disponible, intentar recuperación
            else if (ShouldAttemptRecovery())
            {
                await AttemptRecovery();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed unexpectedly");
        }
        finally
        {
            _healthCheckSemaphore.Release();
        }
    }

    private async Task QuickHealthCheck()
    {
        try
        {
            var testKey = $"{_options.Redis.KeyPrefix}:healthcheck";
            var testValue = DateTime.UtcNow.Ticks.ToString();

            using var cts = new CancellationTokenSource(_options.HealthCheck.HealthCheckTimeout);

            await _distributedCache!.SetStringAsync(testKey, testValue, cts.Token);
            var retrieved = await _distributedCache.GetStringAsync(testKey, cts.Token);
            await _distributedCache.RemoveAsync(testKey, cts.Token);

            if (retrieved == testValue)
            {
                // Reset failure counter on successful operation
                if (_consecutiveFailures > 0)
                {
                    Interlocked.Exchange(ref _consecutiveFailures, 0);
                    _logger.LogDebug("Redis health check passed. Failure counter reset.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Redis health check failed: {Error}", ex.Message);
            await HandleRedisFailure();
        }
    }

    private async Task AttemptRecovery()
    {
        try
        {
            var testKey = $"{_options.Redis.KeyPrefix}:recovery";
            var testValue = "recovery-test";

            using var cts = new CancellationTokenSource(_options.HealthCheck.HealthCheckTimeout);

            await _distributedCache!.SetStringAsync(testKey, testValue, cts.Token);
            var retrieved = await _distributedCache.GetStringAsync(testKey, cts.Token);
            await _distributedCache.RemoveAsync(testKey, cts.Token);

            if (retrieved == testValue)
            {
                _isRedisAvailable = true;
                Interlocked.Exchange(ref _consecutiveFailures, 0);
                _logger.LogInformation(
                    "Redis connection recovered successfully. Switching back to hybrid mode."
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Redis recovery attempt failed: {Error}", ex.Message);
        }
    }

    private bool ShouldAttemptRecovery()
    {
        return DateTime.UtcNow - _lastFailureTime >= _options.HealthCheck.RecoveryInterval;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        _healthCheckTimer?.Dispose();
        _healthCheckSemaphore?.Dispose();

        _logger.LogDebug("HybridCache disposed");
    }
}
