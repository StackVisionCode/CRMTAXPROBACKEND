namespace Domain.Entities;

public class CalendarEvents
{ 
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public Guid? CustomerId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Siempre UTC
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }

    // "appointment" | "meeting" (se configura por TPH)
    public string Type { get; protected set; } = string.Empty;

    public string CreatedBy { get; set; } = string.Empty;

    // Recordatorio “default” (ej: 60 min antes). Los recordatorios reales
    // los programa ReminderService, esto es solo el valor por defecto.
    public TimeSpan ReminderBefore { get; set; } = TimeSpan.FromMinutes(60);

    // Concurrency
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

}