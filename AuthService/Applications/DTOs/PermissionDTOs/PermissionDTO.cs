using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.PermissionDTOs;

public class PermissionDTO
{
    [Key]
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? Description { get; set; }
}
