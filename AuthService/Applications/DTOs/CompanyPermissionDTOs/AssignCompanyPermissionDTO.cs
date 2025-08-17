namespace AuthService.DTOs.CompanyPermissionDTOs;

public class AssignCompanyPermissionDTO
{
    public required Guid TaxUserId { get; set; }
    public required Guid PermissionId { get; set; }
    public bool IsGranted { get; set; } = true;
    public string? Description { get; set; }
}
