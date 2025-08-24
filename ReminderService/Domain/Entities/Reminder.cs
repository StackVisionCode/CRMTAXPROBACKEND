namespace Domain.Entities;

public class Reminder
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // "event" | "task" | "custom"
    public string AggregateType { get; set; } = "custom";
    public Guid? AggregateId { get; set; }

    public string UserId { get; set; } = "unknown";

    // "email" | "sms" | "push"
    public string Channel { get; set; } = "email";

    public string Message { get; set; } = "Tienes un recordatorio";

    public DateTimeOffset RemindAtUtc { get; set; }

    // Opcional (si usaras repetitivos)
    public string? Cron { get; set; }

    // "scheduled" | "sent" | "cancelled" | "failed"
    public string Status { get; set; } = "scheduled";

    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
