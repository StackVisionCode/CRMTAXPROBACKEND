namespace LandingService.Applications.DTO;
public class CreateEventDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public DateTime Date { get; set; }
    public string? Description { get; set; }
    public List<PersonDto> Attendees { get; set; } = new();
    public List<DocumentDto> Documents { get; set; } = new();
}