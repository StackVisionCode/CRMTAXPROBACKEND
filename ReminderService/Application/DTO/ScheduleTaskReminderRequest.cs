// ScheduleTaskReminderRequest.cs
namespace Application.DTO;

public class ScheduleTaskReminderRequest
{
    public int[] DaysBefore { get; set; } = Array.Empty<int>();
    public DateTimeOffset DueAtUtc { get; set; }
    public TimeSpan? RemindAtTime { get; set; }
    public string? Message { get; set; }
    public string? UserId { get; set; }
    public string Channel { get; set; } = "email";
}
