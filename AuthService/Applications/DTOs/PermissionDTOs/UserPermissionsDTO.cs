namespace AuthService.DTOs.PermissionDTOs;

public class UserPermissionsDTO
{
    public Guid UserId { get; set; }
    public ICollection<string> PermissionCodes { get; set; } = new List<string>();
}
