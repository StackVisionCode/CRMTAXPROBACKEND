using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.CompanyPermissionDTOs;

public class CompanyPermissionDTO
{
    [Key]
    public Guid Id { get; set; }
    public required Guid TaxUserId { get; set; }
    public required Guid PermissionId { get; set; }
    public bool IsGranted { get; set; } = true;
    public string? Description { get; set; }

    // Información del User y Permission
    public string? UserEmail { get; set; }
    public string? UserName { get; set; }
    public string? UserLastName { get; set; }
    public string? PermissionCode { get; set; }
    public string? PermissionName { get; set; }
    public DateTime CreatedAt { get; set; }
}
