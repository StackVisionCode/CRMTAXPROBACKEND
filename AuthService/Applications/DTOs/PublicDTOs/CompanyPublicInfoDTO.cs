namespace DTOs.PublicDTOs;

public class CompanyPublicInfoDTO
{
    public Guid Id { get; set; }
    public string? CompanyName { get; set; }
    public string? Brand { get; set; }
    public string? Domain { get; set; }
    public string? Phone { get; set; }

    // Información básica de dirección (sin detalles específicos)
    public string? City { get; set; }
    public string? State { get; set; }
    public string? CountryName { get; set; }

    // Owner info básica
    public Guid? OwnerId { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerLastName { get; set; }
    public string? OwnerPhotoUrl { get; set; }
    public bool OwnerIsActive { get; set; }

    // Plan info muy limitada
    public bool HasActivePlan { get; set; }
    public string? PlanType { get; set; } // Solo Basic/Standard/Pro
}
