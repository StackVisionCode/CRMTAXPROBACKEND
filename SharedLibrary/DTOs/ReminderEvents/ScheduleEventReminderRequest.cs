namespace SharedLibrary.DTOs.Reminders;

public sealed class ScheduleEventReminderRequest
{
    // e.g. [7, 1, 0]
    public int[] DaysBefore { get; set; } = Array.Empty<int>();

    // Inicio del evento en UTC
    public DateTimeOffset EventStartUtc { get; set; }

    // Hora del recordatorio (local del usuario, opcional)
    public TimeSpan? RemindAtTime { get; set; }

    // Mensaje opcional para el recordatorio
    public string? Message { get; set; }

    // Id del usuario (string para ser consistente con tu ReminderDueEvent)
    public string? UserId { get; set; }

    // "email" | "sms" | "push"
    public string Channel { get; set; } = "email";
}
