namespace AuthService.DTOs.CompanyPermissionDTOs;

/// <summary>
/// DTO para permisos de un TaxUser
/// </summary>
public class CompanyUserPermissionsDTO
{
    public Guid TaxUserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? UserLastName { get; set; }
    public bool IsOwner { get; set; }

    public ICollection<CompanyPermissionDTO> CustomPermissions { get; set; } =
        new List<CompanyPermissionDTO>();
    public ICollection<string> RoleBasedPermissions { get; set; } = new List<string>();
    public ICollection<string> EffectivePermissions { get; set; } = new List<string>();
}
