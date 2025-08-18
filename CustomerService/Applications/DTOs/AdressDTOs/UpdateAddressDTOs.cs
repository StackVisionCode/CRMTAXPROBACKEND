using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.AddressDTOs;

public class UpdateAddressDTO
{
    [Key]
    public required Guid Id { get; set; }
    public required Guid CustomerId { get; set; }
    public string? StreetAddress { get; set; }
    public string? ApartmentNumber { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string Country { get; set; } = "USA";
    public Guid? LastModifiedByTaxUserId { get; set; }
}
