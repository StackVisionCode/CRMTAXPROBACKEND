using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedLibrary.Caching;
using StackExchange.Redis;

namespace SharedLibrary.Extensions;

public static class HybridCacheServiceCollectionExtensions
{
    /// <summary>
    /// Agrega el sistema de caché híbrido con Redis y contingencia local.
    /// Esta implementación está optimizada para usar una única instancia de ConnectionMultiplexer
    /// y manejar la reconexión de forma robusta.
    /// </summary>
    public static IServiceCollection AddHybridCache(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // 1. Configurar opciones desde appsettings.json
        services.Configure<HybridCacheOptions>(
            configuration.GetSection(HybridCacheOptions.SectionName)
        );

        var cacheOptions =
            configuration.GetSection(HybridCacheOptions.SectionName).Get<HybridCacheOptions>()
            ?? new HybridCacheOptions();

        // 2. Configurar Memory Cache local como base para el sistema híbrido
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = cacheOptions.Local.SizeLimit;
            options.ExpirationScanFrequency = cacheOptions.Local.ExpirationScanFrequency;
            options.CompactionPercentage = cacheOptions.Local.CompactionPercentage;
        });

        // 3. Configurar Redis y el ConnectionMultiplexer como Singleton
        try
        {
            var connectionString = cacheOptions.Redis.ConnectionString;

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                AddLocalCacheOnly(services, "Redis no está configurado. Usando solo caché local.");
            }
            else
            {
                var configOptions = ConfigurationOptions.Parse(connectionString);
                configOptions.AbortOnConnectFail = false;
                configOptions.ConnectRetry = 5;
                configOptions.ReconnectRetryPolicy = new ExponentialRetry(5000, 15000);
                configOptions.ConnectTimeout = (int)
                    cacheOptions.Redis.ConnectTimeout.TotalMilliseconds;
                configOptions.SyncTimeout = (int)
                    cacheOptions.Redis.CommandTimeout.TotalMilliseconds;
                configOptions.AsyncTimeout = (int)
                    cacheOptions.Redis.CommandTimeout.TotalMilliseconds;
                configOptions.DefaultDatabase = cacheOptions.Redis.Database;
                configOptions.KeepAlive = 180;
                configOptions.ResolveDns = true;

                // Usamos un factory para crear la conexión de forma perezosa y robusta
                services.AddSingleton<IConnectionMultiplexer>(sp =>
                {
                    var logger = sp.GetRequiredService<ILogger<IConnectionMultiplexer>>();
                    logger.LogInformation(
                        "Creando instancia singleton de ConnectionMultiplexer para Redis."
                    );
                    return ConnectionMultiplexer.Connect(configOptions);
                });

                services.AddStackExchangeRedisCache(options =>
                {
                    // Reutilizamos la instancia singleton del ConnectionMultiplexer
                    options.ConnectionMultiplexerFactory = async () =>
                    {
                        var sp = services.BuildServiceProvider();
                        return await Task.FromResult(
                            sp.GetRequiredService<IConnectionMultiplexer>()
                        );
                    };
                    options.InstanceName = cacheOptions.Redis.KeyPrefix;
                });

                services.AddSingleton<IHybridCache>(sp =>
                {
                    var distributedCache = sp.GetService<IDistributedCache>();
                    var memoryCache = sp.GetRequiredService<IMemoryCache>();
                    var options = sp.GetRequiredService<IOptions<HybridCacheOptions>>().Value;
                    var logger = sp.GetRequiredService<ILogger<HybridCache>>();

                    return new HybridCache(distributedCache, memoryCache, options, logger);
                });
            }
        }
        catch (Exception ex)
        {
            AddLocalCacheOnly(
                services,
                $"Error al configurar Redis: {ex.Message}. Usando solo caché local.",
                ex
            );
        }

        return services;
    }

    /// <summary>
    /// ✅ MÉTODO RESTAURADO. Mantiene la compatibilidad con el sistema anterior
    /// y ahora sirve como fallback para el caché híbrido.
    /// </summary>
    public static IServiceCollection AddSessionCache(this IServiceCollection services)
    {
        // Mantener compatibilidad con implementación anterior, usando valores fijos.
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 2048;
            options.ExpirationScanFrequency = TimeSpan.FromMinutes(5);
        });

        return services;
    }

    /// <summary>
    /// Método auxiliar privado para registrar el HybridCache en modo "solo local".
    /// </summary>
    private static void AddLocalCacheOnly(
        IServiceCollection services,
        string logMessage,
        Exception? ex = null
    )
    {
        services.AddSingleton<IHybridCache>(sp =>
        {
            var memoryCache = sp.GetRequiredService<IMemoryCache>();
            var options = sp.GetRequiredService<IOptions<HybridCacheOptions>>().Value;
            var logger = sp.GetRequiredService<ILogger<HybridCache>>();

            if (ex != null)
            {
                logger.LogError(ex, logMessage);
            }
            else
            {
                logger.LogWarning(logMessage);
            }

            // Se instancia HybridCache con el distributedCache en null.
            return new HybridCache(null, memoryCache, options, logger);
        });
    }
}
