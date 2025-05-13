
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CompanyService.Application.DTOs;

public class CompanyDto
{
   [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
   [Key]
   public int Id { get; set; }
   public int? UserId { get; set; }
   public required string CompanyName { get; set; }
   public int? StateId { get; set; }

   public string? Fullname { get; set; }
   public string? Address { get; set; }
   [DataType(DataType.PhoneNumber)]
   public string? PhoneNumber { get; set; }
   public string? Description { get; set; }
   [DataType(DataType.EmailAddress)]
   public required string Email { get; set; }
   [DataType(DataType.Password)]
   [StringLength(100, MinimumLength = 8)]
   public string? Password { get; set; }
   public required bool Login { get; set; }
   public string? Brand { get; set; }
   public required int UserCount { get; set; }
  
}