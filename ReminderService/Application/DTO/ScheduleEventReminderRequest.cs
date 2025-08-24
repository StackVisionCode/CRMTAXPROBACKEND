// ScheduleEventReminderRequest.cs
namespace Application.DTO;

public class ScheduleEventReminderRequest
{
    public int[] DaysBefore { get; set; } = Array.Empty<int>(); // ej: [7,1,0]
    public DateTimeOffset EventStartUtc { get; set; }
    public TimeSpan? RemindAtTime { get; set; } // hora local deseada; si null usa 09:00
    public string? Message { get; set; }
    public string? UserId { get; set; }
    public string Channel { get; set; } = "email";
}
