namespace Infrastructure.Reminders;

public sealed class ReminderClientOptions
{
    // Apunta al API Gateway (Ocelot) o directo al ReminderService
    public string BaseUrl { get; set; } = "http://localhost:5000";

    // Upstream de Ocelot hacia ReminderService (ajústalo a tu gateway)
    public string EventsPathTemplate { get; set; } = "/reminders/api/reminders/events/{eventId}";

    // Hora por defecto del recordatorio (si no se especifica)
    public TimeSpan DefaultRemindAtTime { get; set; } = TimeSpan.FromHours(9);

    // Opcionales para trazabilidad
    public string? DefaultCorrelationId { get; set; }
    public string? UserAgent { get; set; } = "CalendarService-ReminderClient/1.0";
}
