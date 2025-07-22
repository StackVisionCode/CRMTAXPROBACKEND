namespace SharedLibrary.Caching;

/// <summary>
/// Interfaz para el sistema de caché híbrido con contingencia automática
/// </summary>
public interface IHybridCache
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken cancellationToken = default
    );
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);
    bool IsRedisAvailable { get; }
    string CurrentCacheMode { get; }
}

/// <summary>
/// Configuración para el sistema de caché híbrido
/// </summary>
public class HybridCacheOptions
{
    public const string SectionName = "HybridCache";

    /// <summary>
    /// Configuración de Redis
    /// </summary>
    public RedisOptions Redis { get; set; } = new();

    /// <summary>
    /// Configuración de caché local
    /// </summary>
    public LocalCacheOptions Local { get; set; } = new();

    /// <summary>
    /// Configuración de monitoreo de salud
    /// </summary>
    public HealthCheckOptions HealthCheck { get; set; } = new();
}

public class RedisOptions
{
    /// <summary>
    /// Cadena de conexión a Redis
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// Prefijo para todas las claves de caché
    /// </summary>
    public string KeyPrefix { get; set; } = "taxapp";

    /// <summary>
    /// Base de datos Redis a usar (0-15)
    /// </summary>
    public int Database { get; set; } = 0;

    /// <summary>
    /// Tiempo de expiración por defecto
    /// </summary>
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Timeout para operaciones
    /// </summary>
    public TimeSpan CommandTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Timeout para conexión
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);
}

public class LocalCacheOptions
{
    /// <summary>
    /// Límite de tamaño del caché en memoria (entradas)
    /// </summary>
    public long SizeLimit { get; set; } = 4096;

    /// <summary>
    /// Frecuencia de limpieza de entradas expiradas
    /// </summary>
    public TimeSpan ExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Tiempo de expiración por defecto
    /// </summary>
    public TimeSpan DefaultExpiry { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Factor de compactación cuando se alcanza el límite
    /// </summary>
    public double CompactionPercentage { get; set; } = 0.25;
}

public class HealthCheckOptions
{
    /// <summary>
    /// Intervalo entre verificaciones de salud de Redis
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Número de fallos consecutivos antes de marcar Redis como no disponible
    /// </summary>
    public int FailureThreshold { get; set; } = 3;

    /// <summary>
    /// Tiempo después del cual se reintenta conectar a Redis
    /// </summary>
    public TimeSpan RecoveryInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Timeout para el health check
    /// </summary>
    public TimeSpan HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(2);
}
