namespace Application.DTO;
public class CalendarEventUpdateDto
{
    public Guid Id { get; set; }
    public CalendarEventDto Event { get; set; } = default!;
}