namespace Domain.Entities;


public class EventParticipant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid MeetingId { get; set; }
    public Meeting Meeting { get; set; } = default!;
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
}