using System.ComponentModel.DataAnnotations;

namespace UserDTOS;

public class TaxUserProfileDTO
{
    [Key]
    public required Guid TaxUserId { get; set; }
    public required string Name { get; set; }
    public required string LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? PhotoUrl { get; set; }
}
