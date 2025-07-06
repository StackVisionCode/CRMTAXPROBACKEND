using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.RoleDTOs;

public class UserRoleDTO
{
    [Key]
    public Guid Id { get; set; }
    public required Guid TaxUserId { get; set; }
    public required Guid RoleId { get; set; }
}
