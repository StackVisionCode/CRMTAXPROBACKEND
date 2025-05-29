using System.ComponentModel.DataAnnotations;

namespace AuthService.Applications.DTOs.CompanyDTOs;

public class NewCompanyDTO
{
  [Key]
  public Guid Id { get; set; }
  public string? FullName { get; set; }
  public string? CompanyName { get; set; } 
  public string? Address { get; set; }
  public string? Description { get; set; }
  [Range(1, int.MaxValue, ErrorMessage = "UserLimit must be greater than 0.")]
  public int UserLimit { get; set; } 
  [EmailAddress]
  public required string? Email { get; set; }
  public string? Brand { get; set; }
  [MinLength(8)]
  public string? Password { get; set; }
}