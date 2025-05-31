using System.ComponentModel.DataAnnotations;

namespace CustomerService.DTOs.AddressDTOs;

public class ReadAddressDTO
{
    [Key]
    public required Guid Id { get; set; }
    public string? Customer { get; set; }
    public string? StreetAddress { get; set; }
    public string? ApartmentNumber { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }
    public string Country { get; set; } = "USA";
}
