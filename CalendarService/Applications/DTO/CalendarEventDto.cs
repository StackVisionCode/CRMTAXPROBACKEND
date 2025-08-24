namespace Application.DTO;

public class CalendarEventDto
{

    public Guid UserId { get; set; }
    public Guid? CustomerId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // FE manda siempre UTC (ISO8601)
    public DateTimeOffset StartUtc { get; set; }
    public DateTimeOffset EndUtc { get; set; }

    // "appointment" | "meeting"
    public string Type { get; set; } = "appointment";

    public string CreatedBy { get; set; } = string.Empty;
    public TimeSpan ReminderBefore { get; set; } = TimeSpan.FromMinutes(60);
}