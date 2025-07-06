namespace Domain.Entities;
public class Meeting : CalendarEvents
{
    public List<string> Participants { get; set; } = new();
    public string MeetingLink { get; set; } = string.Empty;
}