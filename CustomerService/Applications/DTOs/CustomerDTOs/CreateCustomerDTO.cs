namespace CustomerService.DTOs.CustomerDTOs;

public class CreateCustomerDTO
{
    public required Guid TaxUserId { get; set; }
    public required Guid OccupationId { get; set; }
    public required Guid MaritalStatusId { get; set; }
    public required Guid CustomerTypeId { get; set; }
    public required string FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public required string SsnOrItin { get; set; }
    public required bool IsActive { get; set; }
    public required bool IsLogin { get; set; }
}
