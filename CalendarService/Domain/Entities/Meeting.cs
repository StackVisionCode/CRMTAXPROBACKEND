namespace Domain.Entities;
public class Meeting : CalendarEvents
{
   public Meeting() { Type = "meeting"; }
    public string MeetingLink { get; set; } = string.Empty;
    public ICollection<EventParticipant> Participants { get; set; } = new List<EventParticipant>();
}