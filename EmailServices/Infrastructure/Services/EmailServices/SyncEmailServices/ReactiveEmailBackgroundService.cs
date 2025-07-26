using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ReactiveEmailBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReactiveEmailBackgroundService> _logger;
    private readonly IConfiguration _configuration;

    public ReactiveEmailBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ReactiveEmailBackgroundService> logger,
        IConfiguration configuration
    )
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var isEnabled =
            _configuration.GetValue<bool>("AutoReceive:Enabled", false)
            || _configuration.GetValue<bool>("EmailService:AutoReceive:Enabled", false);

        if (!isEnabled)
        {
            _logger.LogWarning(
                "❌ Reactive email service is DISABLED. Set AutoReceive:Enabled = true to enable"
            );
            return;
        }

        _logger.LogInformation("⚡ Starting REACTIVE email service (no polling intervals)");

        await StartWatchingAllConfigurations(stoppingToken);

        // Mantener el servicio vivo
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Solo para mantener vivo
                await CheckForNewConfigurations(stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in reactive email service main loop");
            }
        }

        _logger.LogInformation("🛑 Reactive email service stopped");
    }

    private async Task StartWatchingAllConfigurations(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EmailContext>();
        var reactiveService =
            scope.ServiceProvider.GetRequiredService<IReactiveEmailReceivingService>();

        try
        {
            var activeConfigs = await context
                .EmailConfigs.Where(c => !string.IsNullOrEmpty(c.ProviderType))
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "⚡ Starting reactive watching for {Count} email configurations",
                activeConfigs.Count
            );

            foreach (var config in activeConfigs)
            {
                try
                {
                    _logger.LogInformation(
                        "👀 Starting watch for {ConfigName} ({ProviderType})",
                        config.Name,
                        config.ProviderType
                    );

                    // Hacer sync inicial para obtener emails recientes
                    var syncResult = await reactiveService.SyncAllEmailsAsync(config.Id);
                    _logger.LogInformation(
                        "📊 Initial sync for {ConfigName}: {Message}",
                        config.Name,
                        syncResult.Message
                    );

                    // Iniciar watching reactivo
                    await reactiveService.StartWatchingAsync(config);

                    _logger.LogInformation(
                        "✅ Started reactive watching for {ConfigName}",
                        config.Name
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "❌ Failed to start watching for config {ConfigName}",
                        config.Name
                    );
                }

                // Pequeña pausa entre configuraciones
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }

            if (!activeConfigs.Any())
            {
                _logger.LogWarning(
                    "⚠️ No email configurations found. Please create at least one EmailConfig."
                );
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error starting reactive email watching");
        }
    }

    private async Task CheckForNewConfigurations(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EmailContext>();
        var reactiveService =
            scope.ServiceProvider.GetRequiredService<IReactiveEmailReceivingService>();

        try
        {
            // Verificar si hay nuevas configuraciones que no estamos observando
            var allConfigs = await context
                .EmailConfigs.Where(c => !string.IsNullOrEmpty(c.ProviderType))
                .ToListAsync(cancellationToken);

            foreach (var config in allConfigs)
            {
                // StartWatchingAsync verifica internamente si ya está siendo observado
                await reactiveService.StartWatchingAsync(config);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error checking for new configurations");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("🛑 Stopping reactive email service...");

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<EmailContext>();
        var reactiveService =
            scope.ServiceProvider.GetRequiredService<IReactiveEmailReceivingService>();

        try
        {
            var activeConfigs = await context
                .EmailConfigs.Select(c => c.Id)
                .ToListAsync(cancellationToken);

            foreach (var configId in activeConfigs)
            {
                await reactiveService.StopWatchingAsync(configId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error stopping reactive watching");
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("✅ Reactive email service stopped completely");
    }
}
