using Domain;
using Infrastructure.Context;
using MailKit;
using MailKit.Net.Imap;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class ReactiveEmailReceivingService : IReactiveEmailReceivingService
{
    private readonly IServiceScopeFactory _scopeFactory; // ⭐ CAMBIO: IServiceScopeFactory en lugar de IServiceProvider
    private readonly ILogger<ReactiveEmailReceivingService> _logger;
    private static readonly Dictionary<Guid, CancellationTokenSource> _watchingTasks = new();

    public ReactiveEmailReceivingService(
        IServiceScopeFactory scopeFactory, // ⭐ CAMBIO: IServiceScopeFactory
        ILogger<ReactiveEmailReceivingService> logger
    )
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task<IEnumerable<IncomingEmail>> CheckAllEmailsAsync(
        EmailConfig config,
        int maxMessages = 100,
        DateTime? since = null
    )
    {
        using var scope = _scopeFactory.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IEmailSyncService>();
        var result = await syncService.SyncEmailsAsync(config.Id, since);

        // Retornar emails desde la base de datos
        var context = scope.ServiceProvider.GetRequiredService<EmailContext>();
        return await context
            .IncomingEmails.Where(e => e.ConfigId == config.Id)
            .OrderByDescending(e => e.ReceivedOn)
            .Take(maxMessages)
            .ToListAsync();
    }

    public async Task<EmailSyncResult> SyncAllEmailsAsync(Guid configId, DateTime? since = null)
    {
        using var scope = _scopeFactory.CreateScope();
        var syncService = scope.ServiceProvider.GetRequiredService<IEmailSyncService>();
        return await syncService.SyncEmailsAsync(configId, since);
    }

    public async Task StartWatchingAsync(EmailConfig config)
    {
        if (_watchingTasks.ContainsKey(config.Id))
        {
            _logger.LogInformation("⚡ Already watching config {ConfigName}", config.Name);
            return;
        }

        var cts = new CancellationTokenSource();
        _watchingTasks[config.Id] = cts;

        _logger.LogInformation("⚡ Starting reactive email watching for {ConfigName}", config.Name);

        if (config.ProviderType.Equals("Gmail", StringComparison.OrdinalIgnoreCase))
        {
            _ = Task.Run(async () => await WatchGmailAsync(config, cts.Token), cts.Token);
        }
        else
        {
            _ = Task.Run(async () => await WatchImapAsync(config, cts.Token), cts.Token);
        }
    }

    public async Task StopWatchingAsync(Guid configId)
    {
        if (_watchingTasks.TryGetValue(configId, out var cts))
        {
            cts.Cancel();
            _watchingTasks.Remove(configId);
            _logger.LogInformation("🛑 Stopped watching config {ConfigId}", configId);
        }
    }

    private async Task WatchImapAsync(EmailConfig config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("👀 Starting IMAP watch for {ConfigName}", config.Name);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var client = new ImapClient();
                var imapServer = GetImapServerFromSmtp(config.SmtpServer ?? "");
                var imapPort = GetImapPortFromSmtp(config.SmtpServer ?? "");

                await client.ConnectAsync(imapServer, imapPort, true, cancellationToken);
                await client.AuthenticateAsync(
                    config.SmtpUsername,
                    config.SmtpPassword,
                    cancellationToken
                );

                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadOnly, cancellationToken);

                if (client.Capabilities.HasFlag(ImapCapabilities.Idle))
                {
                    _logger.LogInformation("⚡ Using IMAP IDLE for {ConfigName}", config.Name);

                    // ⭐ CORRECCIÓN: USAR _scopeFactory EN LUGAR DE _serviceProvider ⭐
                    inbox.CountChanged += (sender, e) =>
                    {
                        _logger.LogInformation(
                            "📧 New email detected for {ConfigName}",
                            config.Name
                        );

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // ⭐ CREAR SCOPE FRESCO CADA VEZ ⭐
                                using var taskScope = _scopeFactory.CreateScope();
                                var syncService =
                                    taskScope.ServiceProvider.GetRequiredService<IEmailSyncService>();
                                var result = await syncService.SyncEmailsAsync(
                                    config.Id,
                                    DateTime.UtcNow.AddMinutes(-5)
                                );

                                _logger.LogInformation(
                                    "✅ Sync completed for {ConfigName}: {Message}",
                                    config.Name,
                                    result.Message
                                );
                            }
                            catch (Exception syncEx)
                            {
                                _logger.LogError(
                                    syncEx,
                                    "❌ Sync error for {ConfigName}: {Error}",
                                    config.Name,
                                    syncEx.Message
                                );
                            }
                        });
                    };

                    await client.IdleAsync(cancellationToken);
                }
                else
                {
                    _logger.LogInformation(
                        "📊 IMAP IDLE not supported, using polling for {ConfigName}",
                        config.Name
                    );
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        try
                        {
                            using var taskScope = _scopeFactory.CreateScope();
                            var syncService =
                                taskScope.ServiceProvider.GetRequiredService<IEmailSyncService>();
                            await syncService.SyncEmailsAsync(
                                config.Id,
                                DateTime.UtcNow.AddMinutes(-5)
                            );
                            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                        }
                        catch (Exception syncEx)
                        {
                            _logger.LogError(
                                syncEx,
                                "❌ Polling sync error for {ConfigName}",
                                config.Name
                            );
                        }
                    }
                }

                await client.DisconnectAsync(true, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "❌ IMAP watching error for {ConfigName}, retrying",
                    config.Name
                );
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }

    private async Task WatchGmailAsync(EmailConfig config, CancellationToken cancellationToken)
    {
        _logger.LogInformation("👀 Starting Gmail polling for {ConfigName}", config.Name);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var syncService = scope.ServiceProvider.GetRequiredService<IEmailSyncService>();
                await syncService.SyncEmailsAsync(config.Id, DateTime.UtcNow.AddMinutes(-5));
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Gmail polling error for {ConfigName}", config.Name);
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }

    private string GetImapServerFromSmtp(string smtpServer) =>
        smtpServer.ToLower() switch
        {
            "smtp.gmail.com" => "imap.gmail.com",
            "smtp.outlook.com" => "imap-mail.outlook.com",
            _ => smtpServer.Replace("smtp", "imap", StringComparison.OrdinalIgnoreCase),
        };

    private int GetImapPortFromSmtp(string smtpServer) =>
        smtpServer.ToLower() switch
        {
            "smtp.gmail.com" => 993,
            _ => 993,
        };
}
