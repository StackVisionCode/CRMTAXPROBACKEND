using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.ContactInfoDTOs;

public class CreateContactInfoDTOs
{
    public Guid CustomerId { get; set; }
    public required string PhoneNumber { get; set; }

    [EmailAddress]
    public required string Email { get; set; }
    public Guid PreferredContactId { get; set; }
    public required Guid CreatedByTaxUserId { get; set; }
}
