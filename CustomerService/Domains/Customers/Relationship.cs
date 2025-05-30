using Common;

namespace CustomerService.Domains.Customers;
public class Relationship : BaseEntity
{
    public required string Name { get; set; } = default!;

    // Relación inversa opcional (si quieres navegar desde la relación a los dependents)
    public virtual List<Dependent> Dependents { get; set; } = new();
}

