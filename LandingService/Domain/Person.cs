using System.ComponentModel.DataAnnotations;

namespace LandingService.Domain;
public class Person
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    public string? Email { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }

    // Foreign key to Event
    public Guid EventId { get; set; }
    public virtual Event Event { get; set; } = null!;
}
