using System.ComponentModel.DataAnnotations;
using Applications.DTOs.CompanyDTOs;

namespace AuthService.Applications.DTOs.CompanyDTOs;

public class UpdateCompanyDTO
{
    [Key]
    public required Guid Id { get; set; }
    public bool IsCompany { get; set; }
    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
    public string? Brand { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public string? Domain { get; set; }
    public AddressDTO? Address { get; set; }
}
