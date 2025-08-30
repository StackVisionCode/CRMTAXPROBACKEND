using AuthService.Applications.Common;

namespace AuthService.DTOs.UserDTOs;

public class InvitationValidationDTO
{
    public bool IsValid { get; set; }
    public string? Email { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyFullName { get; set; }
    public string? CompanyDomain { get; set; }
    public bool? IsCompany { get; set; }
    public ServiceLevel? ServiceLevel { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }

    // Propiedades calculadas
    public bool? IsExpired => ExpiresAt.HasValue ? DateTime.UtcNow > ExpiresAt.Value : null;
    public TimeSpan? TimeRemaining =>
        IsValid && ExpiresAt.HasValue && IsExpired.HasValue && !IsExpired.Value
            ? ExpiresAt.Value - DateTime.UtcNow
            : null;
}
