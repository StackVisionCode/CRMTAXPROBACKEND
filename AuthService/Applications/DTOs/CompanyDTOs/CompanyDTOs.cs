using System.ComponentModel.DataAnnotations;
using Applications.DTOs.CompanyDTOs;

namespace AuthService.Applications.DTOs.CompanyDTOs;

public class CompanyDTO
{
    [Key]
    public Guid Id { get; set; }
    public bool IsCompany { get; set; }
    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
    public string? Brand { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public string? Domain { get; set; }
    public int UserLimit { get; set; }
    public int CurrentUserCount { get; set; }
    public AddressDTO? Address { get; set; }
    public DateTime CreatedAt { get; set; }
}
