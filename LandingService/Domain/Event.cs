
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LandingService.Domain;



public class Event
{
    [Key]
    public Guid Id { get; set; }

    public string? PhotoUrl { get; set; }
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Subtitle { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }
 

 
    public ICollection<Person> Attendees { get; set; } = new List<Person>();
    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<Speaker> Speakers { get; set; } = new List<Speaker>();
    public ICollection<Key> EventKeys { get; set; } = new List<Key>();

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public Event()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    {
        Id = Guid.NewGuid();
    }
}
