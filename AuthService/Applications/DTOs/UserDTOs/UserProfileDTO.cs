using System.ComponentModel.DataAnnotations;
using Applications.DTOs.CompanyDTOs;

namespace AuthService.DTOs.UserDTOs;

public class UserProfileDTO
{
    [Key]
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsOwner { get; set; } = false;
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public AddressDTO? Address { get; set; }
    public string? PhotoUrl { get; set; }
    public ICollection<string> RoleNames { get; set; } = new List<string>();

    // Información de la company
    public string? CompanyFullName { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyBrand { get; set; }
    public bool CompanyIsIndividual { get; set; }
    public string? CompanyDomain { get; set; }
    public AddressDTO? CompanyAddress { get; set; }

    // Información del CustomPlan
    public Guid CustomPlanId { get; set; }
    public decimal CustomPlanPrice { get; set; }
    public bool CustomPlanIsActive { get; set; }
    public ICollection<string> AdditionalModules { get; set; } = new List<string>();
    public ICollection<string> EffectivePermissions { get; set; } = new List<string>();
}
