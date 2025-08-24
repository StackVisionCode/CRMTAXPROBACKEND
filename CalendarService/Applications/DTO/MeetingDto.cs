namespace Application.DTO;
public class MeetingDto : CalendarEventDto
{
    public string MeetingLink { get; set; } = string.Empty;
    public List<ParticipantDto> Participants { get; set; } = new();
}
