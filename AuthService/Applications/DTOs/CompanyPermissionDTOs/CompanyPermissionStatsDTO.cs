namespace AuthService.DTOs.CompanyPermissionDTOs;

/// <summary>
/// DTO para estadísticas de permisos por company
/// </summary>
public class CompanyPermissionStatsDTO
{
    public Guid CompanyId { get; set; }
    public int TotalUsers { get; set; }
    public int UsersWithCustomPermissions { get; set; }
    public int TotalCustomPermissions { get; set; }
    public int GrantedPermissions { get; set; }
    public int RevokedPermissions { get; set; }
    public Dictionary<string, int> MostUsedPermissions { get; set; } = new();
}
