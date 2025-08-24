namespace Domain.Entities;

public class Appointment : CalendarEvents
{
    public Appointment() { Type = "appointment"; }
    public string Location { get; set; } = string.Empty;
    public string WithWhom { get; set; } = string.Empty;
}