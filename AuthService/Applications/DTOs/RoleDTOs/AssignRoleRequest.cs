namespace AuthService.DTOs.RoleDTOs;

/// <summary>
/// DTOs para requests
/// </summary>
public class AssignRoleRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public class RemoveRoleRequest
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
}

public class UpdateUserRolesRequest
{
    public Guid UserId { get; set; }
    public IEnumerable<Guid> RoleIds { get; set; } = new List<Guid>();
}
