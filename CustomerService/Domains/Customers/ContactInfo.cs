using System.ComponentModel.DataAnnotations;
using Common;

namespace CustomerService.Domains.Customers;

public class ContactInfo : BaseEntity
{
  public Guid CustomerId { get; set; }
  public required string PhoneNumber { get; set; }
  [EmailAddress]
  public required string Email { get; set; }
  public Guid PreferredContactId { get; set; }
  public required bool IsLoggin { get; set; }
  public string? PasswordClient { get; set; }
  public virtual Customer? Customer { get; set; }
  public virtual PreferredContact? PreferredContact { get; set; }
}