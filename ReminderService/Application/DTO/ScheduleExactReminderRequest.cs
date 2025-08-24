// ScheduleExactReminderRequest.cs
namespace Application.DTO;

public class ScheduleExactReminderRequest
{
    public DateTimeOffset RemindAtUtc { get; set; }
    public string? Message { get; set; }
    public string? UserId { get; set; }
    public string Channel { get; set; } = "email";

    // para relacionarlo opcionalmente a algo
    public string AggregateType { get; set; } = "custom";
    public Guid? AggregateId { get; set; }
}
