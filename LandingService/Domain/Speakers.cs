namespace LandingService.Domain;

public class Speaker
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public required Guid EventId { get; set; }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Speaker()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        Id = Guid.NewGuid();
    }
    public virtual Event Event { get; set; }
}