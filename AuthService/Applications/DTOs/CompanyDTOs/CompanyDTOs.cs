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
    public AddressDTO? Address { get; set; }
    public DateTime CreatedAt { get; set; }

    // Información del servicio base
    public string? BaseServiceName { get; set; }
    public string? BaseServiceTitle { get; set; }
    public List<string> BaseServiceFeatures { get; set; } = new();
    public decimal BaseServicePrice { get; set; }
    public int BaseServiceUserLimit { get; set; }
    public required Guid CustomPlanId { get; set; }
    public decimal CustomPlanPrice { get; set; }
    public int CustomPlanUserLimit { get; set; }
    public bool CustomPlanIsActive { get; set; }
    public DateTime? CustomPlanStartDate { get; set; }
    public DateTime CustomPlanRenewDate { get; set; }
    public bool CustomPlanIsRenewed { get; set; }

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
    public int TotalUsers => CurrentTaxUserCount;

    // Módulos disponibles
    public ICollection<string> AdditionalModules { get; set; } = new List<string>();
    public ICollection<string> BaseModules { get; set; } = new List<string>();
}
