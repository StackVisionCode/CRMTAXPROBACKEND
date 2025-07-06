namespace Domain.Entities;

public class Appointment : CalendarEvents
{
    public string Location { get; set; } = string.Empty;
    public string WithWhom { get; set; } = string.Empty;
}