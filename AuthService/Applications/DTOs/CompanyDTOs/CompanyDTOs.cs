using System.ComponentModel.DataAnnotations;
using Applications.DTOs.AddressDTOs;
using AuthService.Applications.Common;

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
    public AddressDTO? Address { get; set; }
    public DateTime CreatedAt { get; set; }
    public ServiceLevel ServiceLevel { get; set; }

    // Info del TaxUser (admin)
    public Guid AdminUserId { get; set; }
    public string AdminEmail { get; set; } = string.Empty;
    public string? AdminName { get; set; }
    public string? AdminLastName { get; set; }
    public string? AdminPhoneNumber { get; set; }
    public string? AdminPhotoUrl { get; set; }
    public bool AdminIsActive { get; set; }
    public bool AdminConfirmed { get; set; }
    public AddressDTO? AdminAddress { get; set; }
    public ICollection<string> AdminRoleNames { get; set; } = new List<string>();

    // Contadores
    public int CurrentTaxUserCount { get; set; } // Staff users
    public int ActiveTaxUserCount { get; set; }
    public int OwnerCount { get; set; }
    public int TotalUsers => CurrentTaxUserCount;
}
