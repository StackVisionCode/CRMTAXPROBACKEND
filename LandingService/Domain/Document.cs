using System.ComponentModel.DataAnnotations;

namespace LandingService.Domain;

public class Document
{
    [Key]
    public Guid Id { get; set; } 
    [Required]
    [MaxLength(250)]
    public string FileName { get; set; } = string.Empty;

    public string? FileUrl { get; set; }

    // Foreign key to Event
    public Guid EventId { get; set; }
    public virtual Event Event { get; set; } = null!;
    public Document()
    {
        Id= Guid.NewGuid();

    }
}
