namespace AuthService.DTOs.UserDTOs;

public class InvitationValidationDTO
{
    public bool IsValid { get; set; }
    public string? Email { get; set; }
    public Guid? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyDomain { get; set; }
    public bool? IsCompany { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? ErrorMessage { get; set; }
}
