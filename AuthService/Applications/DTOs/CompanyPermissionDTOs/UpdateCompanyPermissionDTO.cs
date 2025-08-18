using System.ComponentModel.DataAnnotations;

namespace AuthService.DTOs.CompanyPermissionDTOs;

public class UpdateCompanyPermissionDTO
{
    [Key]
    public required Guid Id { get; set; }
    public bool IsGranted { get; set; } = true;
    public string? Description { get; set; }
}
