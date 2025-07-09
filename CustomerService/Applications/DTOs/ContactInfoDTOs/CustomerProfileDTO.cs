namespace CustomerService.DTOs.ContactInfoDTOs;

public class CustomerProfileDTO
{
    public Guid PreparerId { get; set; }
    public Guid CustomerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
}
