using Common;

namespace CustomerService.Domains.Customers;

public class Customer : BaseEntity
{
    // Personal
    public required Guid CompanyId { get; set; }
    public Guid CreatedByTaxUserId { get; set; }
    public Guid? LastModifiedByTaxUserId { get; set; }
    public required Guid OccupationId { get; set; }
    public required Guid CustomerTypeId { get; set; }
    public required string FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public required string SsnOrItin { get; set; }

    public Guid MaritalStatusId { get; set; }
    public required bool IsActive { get; set; }

    // Navegación
    public virtual CustomerType? CustomerType { get; set; }
    public virtual MaritalStatus? MaritalStatus { get; set; }
    public virtual Address? Address { get; set; }
    public virtual ContactInfo? Contact { get; set; }
    public virtual List<Dependent>? Dependents { get; set; }
    public virtual TaxInformation? TaxInfo { get; set; }
    public virtual Occupation? Occupation { get; set; }
}
