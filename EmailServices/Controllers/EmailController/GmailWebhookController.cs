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

                    // ACTUALIZADO: Buscar configuración con validación de IsActive
                    using var scope = HttpContext.RequestServices.CreateScope();
                    var context =
                        scope.ServiceProvider.GetRequiredService<Infrastructure.Context.EmailContext>();

                    var config = await context
                        .EmailConfigs.Where(c =>
                            c.GmailEmailAddress == gmailNotification.EmailAddress && c.IsActive
                        )
                        .FirstOrDefaultAsync();

                    if (config != null)
                    {
                        // ACTUALIZADO: Sincronizar emails con CompanyId
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // Usar el servicio actualizado que requiere CompanyId
                                await _reactiveService.SyncAllEmailsAsync(
                                    config.Id,
                                    config.CompanyId, // NUEVO: CompanyId requerido
                                    DateTime.UtcNow.AddMinutes(-5)
                                );
                                _logger.LogInformation(
                                    "✅ Synced emails for {Email} (Company: {CompanyId}) due to push notification",
                                    gmailNotification.EmailAddress,
                                    config.CompanyId
                                );
                            }
                            catch (Exception syncEx)
                            {
                                _logger.LogError(
                                    syncEx,
                                    "❌ Error syncing emails after push notification for {Email} (Company: {CompanyId})",
                                    gmailNotification.EmailAddress,
                                    config.CompanyId
                                );
                            }
                        });
                    }
                    else
                    {
                        _logger.LogWarning(
                            "⚠️ No active configuration found for Gmail address: {Email}",
                            gmailNotification.EmailAddress
                        );
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