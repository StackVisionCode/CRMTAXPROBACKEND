using Application.DTO;

namespace Applications.DTO;

public class CalendarEventDtoGeneral
{
   public Guid Id { get; set; }
    public CalendarEventDto CalendarEvent { get; set; } = default!;
}