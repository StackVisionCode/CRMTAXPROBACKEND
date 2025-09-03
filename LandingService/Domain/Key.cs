namespace LandingService.Domain;

public class Key
{

    public Guid Id { get; set; }
    public string? Name { get; set; }
      public Guid EventId { get; set; }

    public Key()
    {
        Id = Guid.NewGuid();

    }

    public virtual Event Event { get; set; } = null!;
}