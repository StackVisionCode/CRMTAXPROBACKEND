using Common;

namespace CustomerService.Domains.Customers;

public class FilingStatus : BaseEntity
{
    public required string Name { get; set; }

    // Navegación inversa si deseas
    public List<TaxInformation> TaxInformations { get; set; } = new();
}
