using System.ComponentModel.DataAnnotations;

namespace Applications.DTOs.CompanyDTOs;

public class AddressDTO
{
    [Key]
    public int CountryId { get; set; } // 220 = Estados Unidos
    public int StateId { get; set; } // 1..51
    public string? City { get; set; }
    public string? Street { get; set; }
    public string? Line { get; set; }
    public string? ZipCode { get; set; }
    public string? CountryName { get; set; }
    public string? StateName { get; set; }
}
