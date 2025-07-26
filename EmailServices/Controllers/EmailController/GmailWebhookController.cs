using System.Text.Json;
using Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GmailWebhookController : ControllerBase
{
    private readonly IReactiveEmailReceivingService _reactiveService;
    private readonly ILogger<GmailWebhookController> _logger;

    public GmailWebhookController(
        IReactiveEmailReceivingService reactiveService,
        ILogger<GmailWebhookController> logger
    )
    {
        _reactiveService = reactiveService;
        _logger = logger;
    }

    /// <summary>
    /// Webhook para recibir notificaciones push de Gmail
    /// </summary>
    [HttpPost("push-notification")]
    public async Task<IActionResult> ReceivePushNotification()
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            _logger.LogInformation("📬 Received Gmail push notification");
            _logger.LogDebug("Notification body: {Body}", body);

            // Parsear la notificación de Pub/Sub
            var notification = JsonSerializer.Deserialize<PubSubMessage>(body);

            if (notification?.Message?.Data != null)
            {
                // Decodificar el mensaje base64
                var messageBytes = Convert.FromBase64String(notification.Message.Data);
                var messageJson = System.Text.Encoding.UTF8.GetString(messageBytes);

                var gmailNotification = JsonSerializer.Deserialize<GmailNotification>(messageJson);

                if (gmailNotification?.EmailAddress != null)
                {
                    _logger.LogInformation(
                        "📧 Gmail notification for email: {Email}",
                        gmailNotification.EmailAddress
                    );

                    // Buscar configuración correspondiente y sincronizar
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var context =
                        scope.ServiceProvider.GetRequiredService<Infrastructure.Context.EmailContext>();

                    var config = await context.EmailConfigs.FirstOrDefaultAsync(c =>
                        c.GmailEmailAddress == gmailNotification.EmailAddress
                    );

                    if (config != null)
                    {
                        // Sincronizar emails de forma asíncrona
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _reactiveService.SyncAllEmailsAsync(
                                    config.Id,
                                    DateTime.UtcNow.AddMinutes(-5)
                                );
                                _logger.LogInformation(
                                    "✅ Synced emails for {Email} due to push notification",
                                    gmailNotification.EmailAddress
                                );
                            }
                            catch (Exception syncEx)
                            {
                                _logger.LogError(
                                    syncEx,
                                    "❌ Error syncing emails after push notification for {Email}",
                                    gmailNotification.EmailAddress
                                );
                            }
                        });
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing Gmail push notification");
            return StatusCode(500);
        }
    }
}

// Clases para deserializar notificaciones de Pub/Sub
public class PubSubMessage
{
    public Message? Message { get; set; }
}

public class Message
{
    public string? Data { get; set; }
    public Dictionary<string, string>? Attributes { get; set; }
}

public class GmailNotification
{
    public string? EmailAddress { get; set; }
    public long? HistoryId { get; set; }
}
