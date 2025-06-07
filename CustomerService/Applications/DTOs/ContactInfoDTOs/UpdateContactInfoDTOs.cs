using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.ContactInfoDTOs;

public class UpdateContactInfoDTOs
{
    [Key]
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public required string PhoneNumber { get; set; }

    [EmailAddress]
    public required string Email { get; set; }
    public Guid PreferredContactId { get; set; }
    public required bool IsLoggin { get; set; }
    public string? PasswordClient { get; set; }
}
