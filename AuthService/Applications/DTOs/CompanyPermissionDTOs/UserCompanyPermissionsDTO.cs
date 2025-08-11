namespace AuthService.DTOs.CompanyPermissionDTOs;

public class UserCompanyPermissionsDTO
{
    public Guid UserCompanyId { get; set; }
    public string UserCompanyEmail { get; set; } = string.Empty;
    public ICollection<CompanyPermissionDTO> CustomPermissions { get; set; } =
        new List<CompanyPermissionDTO>();
    public ICollection<string> RoleBasedPermissions { get; set; } = new List<string>();
    public ICollection<string> EffectivePermissions { get; set; } = new List<string>();
}
