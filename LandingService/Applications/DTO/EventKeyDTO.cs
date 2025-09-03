namespace LandingService.Applications.DTO;

public class EventKeyDTO
{
    public Guid? Id { get; set; }
    public string Keyword { get; set; } = string.Empty;
    public required Guid EventId { get; set; }
}