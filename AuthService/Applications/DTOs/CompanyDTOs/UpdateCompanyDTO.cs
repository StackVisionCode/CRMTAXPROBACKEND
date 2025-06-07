using System.ComponentModel.DataAnnotations;

namespace AuthService.Applications.DTOs.CompanyDTOs;

public class UpdateCompanyDTO
{
    [Key]
    public required Guid Id { get; set; }

    public string? FullName { get; set; }
    public string? CompanyName { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "UserLimit must be greater than 0.")]
    public int UserLimit { get; set; }
    public string? Domain { get; set; }

    [EmailAddress]
    public string? Email { get; set; }
    public string? Brand { get; set; }

    [MinLength(8)]
    public string? Password { get; set; }
    public bool? IsActive { get; set; }
}
