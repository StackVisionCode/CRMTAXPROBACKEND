namespace Application.DTO;
public class AppointmentDto : CalendarEventDto
{
    public string Location { get; set; } = string.Empty;
    public string WithWhom { get; set; } = string.Empty;
}
