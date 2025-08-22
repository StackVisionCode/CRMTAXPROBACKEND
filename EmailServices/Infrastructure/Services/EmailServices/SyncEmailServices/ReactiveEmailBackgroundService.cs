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
            // ACTUALIZADO: Filtrar solo configuraciones activas
            var activeConfigs = await context
                .EmailConfigs.Where(c => !string.IsNullOrEmpty(c.ProviderType) && c.IsActive)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "⚡ Starting reactive watching for {Count} active email configurations",
                activeConfigs.Count
            );

            foreach (var config in activeConfigs)
            {
                try
                {
                    _logger.LogInformation(
                        "👀 Starting watch for {ConfigName} ({ProviderType}) - Company: {CompanyId}",
                        config.Name,
                        config.ProviderType,
                        config.CompanyId
                    );

                    // Hacer sync inicial con CompanyId
                    var syncResult = await reactiveService.SyncAllEmailsAsync(
                        config.Id,
                        config.CompanyId
                    );
                    _logger.LogInformation(
                        "📊 Initial sync for {ConfigName} (Company: {CompanyId}): {Message}",
                        config.Name,
                        config.CompanyId,
                        syncResult.Message
                    );

                    // Iniciar watching reactivo
                    await reactiveService.StartWatchingAsync(config);

                    _logger.LogInformation(
                        "Started reactive watching for {ConfigName} (Company: {CompanyId})",
                        config.Name,
                        config.CompanyId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "❌ Failed to start watching for config {ConfigName} (Company: {CompanyId})",
                        config.Name,
                        config.CompanyId
                    );
                }

                // Pequeña pausa entre configuraciones
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }

            if (!activeConfigs.Any())
            {
                _logger.LogWarning(
                    "⚠️ No active email configurations found. Please create at least one active EmailConfig."
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
            // ACTUALIZADO: Verificar si hay nuevas configuraciones activas que no estamos observando
            var allConfigs = await context
                .EmailConfigs.Where(c => !string.IsNullOrEmpty(c.ProviderType) && c.IsActive)
                .ToListAsync(cancellationToken);

            foreach (var config in allConfigs)
            {
                // StartWatchingAsync verifica internamente si ya está siendo observado
                await reactiveService.StartWatchingAsync(config);
            }

            _logger.LogDebug(
                "🔍 Checked for new configurations: {Count} active configs found",
                allConfigs.Count
            );
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
            // ACTUALIZADO: Obtener solo IDs de configuraciones activas
            var activeConfigIds = await context
                .EmailConfigs.Where(c => c.IsActive)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            _logger.LogInformation(
                "🛑 Stopping watching for {Count} configurations",
                activeConfigIds.Count
            );

            foreach (var configId in activeConfigIds)
            {
                try
                {
                    await reactiveService.StopWatchingAsync(configId);
                    _logger.LogDebug("🛑 Stopped watching config {ConfigId}", configId);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        ex,
                        "⚠️ Error stopping watch for config {ConfigId}",
                        configId
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error stopping reactive watching");
        }

        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Reactive email service stopped completely");
    }
}
