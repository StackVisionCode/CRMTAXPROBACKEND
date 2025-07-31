using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SharedLibrary.Services.RabbitMQ;

public sealed class RabbitMQStartupService : IHostedService
{
    private readonly IRabbitMQPersistentConnection _connection;
    private readonly RabbitMQConnectionState _connectionState;
    private readonly ILogger<RabbitMQStartupService> _logger;

    public RabbitMQStartupService(
        IRabbitMQPersistentConnection connection,
        RabbitMQConnectionState connectionState,
        ILogger<RabbitMQStartupService> logger
    )
    {
        _connection = connection;
        _connectionState = connectionState;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🚀 Iniciando RabbitMQ Startup Service...");

        var maxAttempts = 10; // 20 segundos máximo
        var delay = TimeSpan.FromSeconds(2);

        for (
            int attempt = 1;
            attempt <= maxAttempts && !cancellationToken.IsCancellationRequested;
            attempt++
        )
        {
            try
            {
                if (_connection.TryConnect())
                {
                    _logger.LogInformation(
                        "✅ RabbitMQ conectado en startup (intento {Attempt})",
                        attempt
                    );
                    _connectionState.IsConnected = true;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "❌ Error en intento {Attempt} de conexión startup", attempt);
            }

            if (attempt < maxAttempts)
            {
                _logger.LogDebug(
                    "⏳ RabbitMQ no disponible, reintentando en {Delay}s... (intento {Attempt}/{MaxAttempts})",
                    delay.TotalSeconds,
                    attempt,
                    maxAttempts
                );
                await Task.Delay(delay, cancellationToken);
            }
        }

        _logger.LogWarning(
            "⚠️ RabbitMQ no disponible después de {MaxAttempts} intentos en startup",
            maxAttempts
        );
        _logger.LogInformation(
            "ℹ️ Microservicio iniciará sin messaging - eventos se encolarán hasta reconexión"
        );
        _connectionState.IsConnected = false;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 RabbitMQ Startup Service detenido");
        return Task.CompletedTask;
    }
}
