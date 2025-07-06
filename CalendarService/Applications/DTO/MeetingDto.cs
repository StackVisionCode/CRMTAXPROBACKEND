namespace Application.DTO;
public class MeetingDto : CalendarEventDto
{
    public List<string> Participants { get; set; } = new();
    public string MeetingLink { get; set; } = string.Empty;
}
