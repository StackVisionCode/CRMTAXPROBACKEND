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
    public int CurrentUserCount { get; set; }
    public AddressDTO? Address { get; set; }
    public DateTime CreatedAt { get; set; }

    // Información del servicio base
    public string? BaseServiceName { get; set; }
    public decimal BaseServicePrice { get; set; }
    public int BaseServiceUserLimit { get; set; }
    public required Guid CustomPlanId { get; set; }
    public decimal CustomPlanPrice { get; set; }
    public bool CustomPlanIsActive { get; set; }
    public DateTime? CustomPlanStartDate { get; set; }
    public DateTime? CustomPlanEndDate { get; set; }
    public DateTime CustomPlanRenewDate { get; set; }
    public bool CustomPlanIsRenewed { get; set; }

    // Contadores
    public int CurrentTaxUserCount { get; set; } // Staff users
    public int CurrentUserCompanyCount { get; set; } // Company users
    public int TotalUsers => CurrentTaxUserCount + CurrentUserCompanyCount;

    // Módulos disponibles
    public ICollection<string> AdditionalModules { get; set; } = new List<string>();
    public ICollection<string> BaseModules { get; set; } = new List<string>();
}
