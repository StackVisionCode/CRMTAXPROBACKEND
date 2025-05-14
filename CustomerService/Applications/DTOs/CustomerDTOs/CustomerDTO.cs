using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CustomerService.DTOs.CustomerDTOs;

public class CustomerDTO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int TaxUserId { get; set; }
    public int ContactId { get; set; }
    public int TeamMemberId { get; set; }
    public string? Name { get; set; }
    public string? LastName { get; set; }
    public required string SSN { get; set; }
    public required string Email  { get; set; }
    public required int CustomerTypeId { get; set; }
}