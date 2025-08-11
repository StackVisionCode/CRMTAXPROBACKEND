namespace AuthService.DTOs.CompanyPermissionDTOs;

public class AssignCompanyPermissionDTO
{
    public required Guid UserCompanyId { get; set; }
    public required Guid UserCompanyRoleId { get; set; }
    public required string Name { get; set; }
    public required string Code { get; set; }
    public bool IsGranted { get; set; } = true;
    public string? Description { get; set; }
}
