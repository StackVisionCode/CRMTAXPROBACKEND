namespace LandingService.Applications.DTO;

public class SpeakerDTO
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public required Guid EventId { get; set; }
}       