namespace AuthService.Domains.Geography;

public class Country // Id INT (220 = Estados Unidos)
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeleteAt { get; set; }
    public ICollection<State> States { get; set; } = new List<State>();
}

public class State // Id INT (1..51) y CountryId = 220
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int CountryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeleteAt { get; set; }
    public Country Country { get; set; } = null!;
}
