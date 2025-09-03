using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Common;

namespace LandingService.Applications.DTO;
public class RegisterDTO:BaseEntity
{

     public required string Name { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string?  CompanyName { get; set; }
    public required string Password { get; set; }
    public string? PhoneNumber { get; set; }
  

}