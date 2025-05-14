using System.Net.Http.Json;

public interface IWebhookNotifier
{
    Task NotifyAuthEventAsync(string eventType, object data, DateTime Expired, string? additionalData = null);
}

public class WebhookNotifierService : IWebhookNotifier
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookNotifierService> _logger;
    private readonly string _signDocuTaxWebhookUrl = "http://localhost:5066/api/webhooks/auth/events";

    public WebhookNotifierService(HttpClient httpClient, ILogger<WebhookNotifierService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task NotifyAuthEventAsync(string eventType, object data, DateTime Expired, string? additionalData = null)
    {
        try
        {
            var payload = new AuthEventRequest
            {
                EventType = eventType,
                data = data,
                Expired = Expired,
                AdditionalData = additionalData
            };

            var response = await _httpClient.PostAsJsonAsync(_signDocuTaxWebhookUrl, payload);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation($"Evento {eventType} notificado a SignDocuTax");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al notificar evento a SignDocuTax");
        }
    }

    public class AuthEventRequest
    {
        public string EventType { get; set; } // "UserAuthenticated", "TokenRevoked", etc.
        public object data { get; set; }

        public DateTime Expired { get; set; }
        public string? AdditionalData { get; set; }
    }
}