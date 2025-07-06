namespace SharedLibrary.Quartz.Models;

public class ReminderJobData
{
    public string Message { get; set; } = string.Empty;
    public DateTime ExecuteAt { get; set; }
}

