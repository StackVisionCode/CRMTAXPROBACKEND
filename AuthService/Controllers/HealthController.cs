using Microsoft.AspNetCore.Mvc;
using SharedLibrary.Caching;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IHybridCache _cache;
    private readonly ILogger<HealthController> _logger;

    public HealthController(IHybridCache cache, ILogger<HealthController> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Test básico de caché para health check
            var testKey = $"health_{DateTime.UtcNow.Ticks}";
            await _cache.SetAsync(testKey, "healthy", TimeSpan.FromSeconds(5));
            var value = await _cache.GetAsync<string>(testKey);
            await _cache.RemoveAsync(testKey);

            var health = new
            {
                Status = value == "healthy" ? "Healthy" : "Unhealthy",
                Service = "AuthService",
                Cache = new
                {
                    Mode = _cache.CurrentCacheMode,
                    RedisAvailable = _cache.IsRedisAvailable,
                },
                Timestamp = DateTime.UtcNow,
                Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "Unknown",
            };

            var statusCode = health.Status == "Healthy" ? 200 : 503;

            return StatusCode(statusCode, health);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");

            return StatusCode(
                503,
                new
                {
                    Status = "Unhealthy",
                    Service = "AuthService",
                    Error = ex.Message,
                    Timestamp = DateTime.UtcNow,
                }
            );
        }
    }
}
