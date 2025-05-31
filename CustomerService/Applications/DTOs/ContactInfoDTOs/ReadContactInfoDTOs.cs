using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.ContactInfoDTOs;

public class ReadContactInfoDTO
{
    [Key]
    public required Guid Id { get; set; }
    public string? Customer { get; set; }
    public string? PreferredContact { get; set; }
    public required string PhoneNumber { get; set; }

    [EmailAddress]
    public required string Email { get; set; }
    public required bool IsLoggin { get; set; }
    public string? PasswordClient { get; set; }
}
