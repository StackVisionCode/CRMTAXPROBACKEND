using System.ComponentModel.DataAnnotations;
using Applications.DTOs.CompanyDTOs;
using AuthService.Applications.Common;
using AuthService.DTOs.CustomModuleDTOs;

namespace AuthService.Applications.DTOs.CompanyDTOs;

public class NewCompanyDTO
{
    public bool IsCompany { get; set; }
    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
    public string? Brand { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }

    public required string Domain { get; set; }
    public AddressDTO? Address { get; set; }

    // CustomPlan info (se crea automáticamente)
    public ServiceLevel? ServiceLevel { get; set; }
    public decimal? CustomPrice { get; set; } // Precio personalizado (opcional)
    public DateTime? PlanStartDate { get; set; }
    public DateTime? PlanEndDate { get; set; }
    public ICollection<NewCustomModuleDTO>? AdditionalModules { get; set; }

    // Admin user info
    [EmailAddress]
    public required string Email { get; set; }

    [MinLength(6)]
    public required string Password { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }

    [Phone]
    public string? PhoneNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public AddressDTO? AdminAddress { get; set; }
}
