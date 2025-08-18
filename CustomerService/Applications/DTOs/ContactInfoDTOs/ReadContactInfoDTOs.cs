using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.ContactInfoDTOs;

public class ReadContactInfoDTO
{
    [Key]
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public required string PhoneNumber { get; set; }
    public required string Email { get; set; }
    public Guid PreferredContactId { get; set; }
    public required bool IsLoggin { get; set; }
    public string? PasswordClient { get; set; }
    public string? Customer { get; set; }
    public string? PreferredContact { get; set; }

    // Información de auditoría
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByTaxUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? LastModifiedByTaxUserId { get; set; }
}
