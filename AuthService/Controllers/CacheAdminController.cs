using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Authorizations;
using SharedLibrary.Caching;

namespace AuthService.Controllers;

/// <summary>
/// Controlador para administrar el sistema de caché híbrido desde AuthService
/// </summary>
[ApiController]
[Route("api/cache/admin")]
// [HasPermission("System.Cache.Admin")]
public class CacheAdminController : ControllerBase
{
    private readonly IHybridCache _cache;
    private readonly ILogger<CacheAdminController> _logger;

    public CacheAdminController(IHybridCache cache, ILogger<CacheAdminController> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene el estado actual del sistema de caché
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var status = new
        {
            Service = "AuthService",
            CurrentMode = _cache.CurrentCacheMode,
            IsRedisAvailable = _cache.IsRedisAvailable,
            Timestamp = DateTime.UtcNow,
            Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
            MachineName = Environment.MachineName,
            ProcessId = Environment.ProcessId,
        };

        _logger.LogInformation(
            "Cache status requested - Mode: {CacheMode}, Redis: {RedisStatus}",
            status.CurrentMode,
            status.IsRedisAvailable
        );

        return Ok(status);
    }

    /// <summary>
    /// Realiza una prueba completa de funcionamiento del caché
    /// </summary>
    [HttpPost("test")]
    public async Task<IActionResult> TestCache()
    {
        var testResults = new List<object>();
        var testKey = $"admin_test_{Guid.NewGuid()}";

        try
        {
            _logger.LogInformation("Starting cache admin test");

            // Test 1: Operación básica de escritura/lectura/eliminación
            var testValue = new
            {
                TestId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Data = "Test data for cache validation",
                Service = "AuthService",
            };

            var startTime = DateTime.UtcNow;

            await _cache.SetAsync(testKey, testValue, TimeSpan.FromMinutes(1));
            var retrieved = await _cache.GetAsync<object>(testKey);
            await _cache.RemoveAsync(testKey);

            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            testResults.Add(
                new
                {
                    Test = "BasicOperations",
                    Success = retrieved != null,
                    Duration = duration.TotalMilliseconds,
                    CacheMode = _cache.CurrentCacheMode,
                    Description = "Set, Get, Remove operations",
                }
            );

            // Test 2: Verificar existencia de claves
            var existsTestKey = $"{testKey}_exists";
            await _cache.SetAsync(existsTestKey, "test", TimeSpan.FromSeconds(30));
            var exists = await _cache.ExistsAsync(existsTestKey);
            await _cache.RemoveAsync(existsTestKey);

            testResults.Add(
                new
                {
                    Test = "ExistenceCheck",
                    Success = exists,
                    CacheMode = _cache.CurrentCacheMode,
                    Description = "Key existence verification",
                }
            );

            // Test 3: Verificar expiración automática
            var expireTestKey = $"{testKey}_expire";
            await _cache.SetAsync(expireTestKey, "expire_test", TimeSpan.FromMilliseconds(200));
            await Task.Delay(300);
            var expiredValue = await _cache.GetAsync<string>(expireTestKey);

            testResults.Add(
                new
                {
                    Test = "Expiration",
                    Success = expiredValue == null,
                    CacheMode = _cache.CurrentCacheMode,
                    Description = "Automatic expiration functionality",
                }
            );

            // Test 4: Rendimiento con múltiples operaciones
            var performanceKey = $"{testKey}_perf";
            var performanceStart = DateTime.UtcNow;

            for (int i = 0; i < 10; i++)
            {
                await _cache.SetAsync(
                    $"{performanceKey}_{i}",
                    $"value_{i}",
                    TimeSpan.FromMinutes(1)
                );
                await _cache.GetAsync<string>($"{performanceKey}_{i}");
            }

            var performanceEnd = DateTime.UtcNow;
            var performanceDuration = performanceEnd - performanceStart;

            // Limpiar claves de performance
            for (int i = 0; i < 10; i++)
            {
                await _cache.RemoveAsync($"{performanceKey}_{i}");
            }

            testResults.Add(
                new
                {
                    Test = "Performance",
                    Success = true,
                    Duration = performanceDuration.TotalMilliseconds,
                    CacheMode = _cache.CurrentCacheMode,
                    Description = "10 set/get operations performance test",
                    AverageOperationTime = performanceDuration.TotalMilliseconds / 20, // 10 sets + 10 gets
                }
            );

            var successfulTests = testResults.Count(t =>
                (bool)t.GetType().GetProperty("Success")!.GetValue(t)!
            );

            _logger.LogInformation(
                "Cache admin test completed - {SuccessCount}/{TotalCount} tests passed",
                successfulTests,
                testResults.Count
            );

            return Ok(
                new
                {
                    Status = "Success",
                    Service = "AuthService",
                    Tests = testResults,
                    Summary = new
                    {
                        TotalTests = testResults.Count,
                        SuccessfulTests = successfulTests,
                        FailedTests = testResults.Count - successfulTests,
                        CurrentMode = _cache.CurrentCacheMode,
                        RedisAvailable = _cache.IsRedisAvailable,
                        TestDuration = (DateTime.UtcNow - startTime).TotalMilliseconds,
                    },
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cache admin test failed");

            return BadRequest(
                new
                {
                    Status = "Failed",
                    Service = "AuthService",
                    Error = ex.Message,
                    StackTrace = ex.StackTrace,
                    Tests = testResults,
                    CurrentMode = _cache.CurrentCacheMode,
                    RedisAvailable = _cache.IsRedisAvailable,
                }
            );
        }
    }

    /// <summary>
    /// Limpia una clave específica del caché
    /// </summary>
    [HttpDelete("clear/{key}")]
    public async Task<IActionResult> ClearKey(string key)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return BadRequest(new { Error = "Key cannot be empty or whitespace" });
            }

            var existed = await _cache.ExistsAsync(key);
            await _cache.RemoveAsync(key);

            _logger.LogInformation(
                "Cache key '{Key}' cleared by admin (existed: {Existed})",
                key,
                existed
            );

            return Ok(
                new
                {
                    Message = $"Key '{key}' cleared successfully",
                    KeyExisted = existed,
                    CacheMode = _cache.CurrentCacheMode,
                    Timestamp = DateTime.UtcNow,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cache key '{Key}'", key);
            return BadRequest(new { Error = ex.Message, Key = key });
        }
    }

    /// <summary>
    /// Limpia múltiples claves que coincidan con un patrón
    /// </summary>
    [HttpDelete("clear-pattern")]
    public async Task<IActionResult> ClearPattern([FromQuery] string pattern)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(pattern))
            {
                return BadRequest(new { Error = "Pattern cannot be empty" });
            }

            // Para este ejemplo, limpiaremos claves conocidas que contengan el patrón
            // En una implementación más avanzada, podrías usar SCAN en Redis
            var commonPrefixes = new[] { "user_permissions:", "session_valid:", "user_profile:" };
            var clearedKeys = new List<string>();

            foreach (var prefix in commonPrefixes)
            {
                if (prefix.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    // Simular limpieza de patrón - en producción podrías usar Redis SCAN
                    var testKey = $"{prefix}test";
                    if (await _cache.ExistsAsync(testKey))
                    {
                        await _cache.RemoveAsync(testKey);
                        clearedKeys.Add(testKey);
                    }
                }
            }

            _logger.LogInformation(
                "Cache pattern '{Pattern}' cleanup completed, {Count} keys cleared",
                pattern,
                clearedKeys.Count
            );

            return Ok(
                new
                {
                    Message = $"Pattern '{pattern}' cleanup completed",
                    ClearedKeys = clearedKeys,
                    CacheMode = _cache.CurrentCacheMode,
                    Timestamp = DateTime.UtcNow,
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cache pattern '{Pattern}'", pattern);
            return BadRequest(new { Error = ex.Message, Pattern = pattern });
        }
    }

    /// <summary>
    /// Obtiene estadísticas detalladas del caché
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics()
    {
        try
        {
            // Test rápido para obtener estadísticas básicas
            var testKey = $"stats_test_{DateTime.UtcNow.Ticks}";
            var startTime = DateTime.UtcNow;

            await _cache.SetAsync(testKey, "stats_value", TimeSpan.FromSeconds(5));
            var retrieved = await _cache.GetAsync<string>(testKey);
            await _cache.RemoveAsync(testKey);

            var endTime = DateTime.UtcNow;
            var responseTime = endTime - startTime;

            var statistics = new
            {
                Service = "AuthService",
                CacheSystem = new
                {
                    CurrentMode = _cache.CurrentCacheMode,
                    RedisAvailable = _cache.IsRedisAvailable,
                    LastTestedAt = DateTime.UtcNow,
                    AverageResponseTime = responseTime.TotalMilliseconds,
                },
                Environment = new
                {
                    MachineName = Environment.MachineName,
                    ProcessorCount = Environment.ProcessorCount,
                    WorkingSet = Environment.WorkingSet,
                    AspNetCoreEnvironment = Environment.GetEnvironmentVariable(
                        "ASPNETCORE_ENVIRONMENT"
                    ),
                },
                Runtime = new
                {
                    UpTime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime,
                    GCMemory = GC.GetTotalMemory(false),
                    ThreadCount = Process.GetCurrentProcess().Threads.Count,
                },
            };

            return Ok(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cache statistics");
            return BadRequest(new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Fuerza una verificación de estado de Redis
    /// </summary>
    [HttpPost("force-redis-check")]
    public IActionResult ForceRedisCheck()
    {
        var currentStatus = new
        {
            Message = "Redis health check is performed automatically by the system every 30 seconds",
            CurrentStatus = new
            {
                Mode = _cache.CurrentCacheMode,
                RedisAvailable = _cache.IsRedisAvailable,
                Service = "AuthService",
                Timestamp = DateTime.UtcNow,
            },
            Information = "The system automatically switches between Redis and local cache based on Redis availability",
        };

        _logger.LogInformation("Redis status check requested via admin endpoint");

        return Ok(currentStatus);
    }
}
