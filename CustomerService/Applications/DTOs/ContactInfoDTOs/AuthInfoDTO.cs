namespace CustomerService.DTOs.ContactInfoDTOs;

/// <summary>DTO mínimo que AuthService necesita para autenticar a un cliente.</summary>
public class AuthInfoDTO
{
    public Guid CompanyId { get; set; }
    public Guid CustomerId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsLogin { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}
