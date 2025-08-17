namespace AuthService.DTOs.CompanyPermissionDTOs;

/// <summary>
/// Request DTO para BulkAssign (ACTUALIZADO)
/// </summary>
public class BulkAssignCompanyPermissionsRequest
{
    public Guid TaxUserId { get; set; }
    public ICollection<AssignCompanyPermissionDTO> Permissions { get; set; } =
        new List<AssignCompanyPermissionDTO>();
}
