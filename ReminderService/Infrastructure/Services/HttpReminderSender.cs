using System.Net.Http.Json;
using Domain.Entities;

namespace Infrastructure.Services;

// Implementa el envío hacia tu EmailServices/CommLinkService (ajusta ruta)
public class HttpReminderSender(HttpClient http) : IReminderSender
{
    public Task SendAsync(Reminder r, CancellationToken ct = default)
        => http.PostAsJsonAsync("/api/notify", new
        {
            userId = r.UserId,
            channel = r.Channel,
            subject = "[TaxPro] Recordatorio",
            message = r.Message,
            payload = new { r.AggregateType, r.AggregateId, r.RemindAtUtc }
        }, ct);
}
