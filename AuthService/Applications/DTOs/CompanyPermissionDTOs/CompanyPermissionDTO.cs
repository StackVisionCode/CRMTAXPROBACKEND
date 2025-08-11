using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.CompanyPermissionDTOs;

public class CompanyPermissionDTO
{
    [Key]
    public Guid Id { get; set; }
    public required Guid UserCompanyId { get; set; }
    public required Guid UserCompanyRoleId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public bool IsGranted { get; set; } = true;
    public string? Description { get; set; }
    public string? UserCompanyEmail { get; set; }
    public string? RoleName { get; set; }
    public DateTime CreatedAt { get; set; }
}
