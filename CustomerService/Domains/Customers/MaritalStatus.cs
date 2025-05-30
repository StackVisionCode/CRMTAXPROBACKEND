
using Common;

namespace CustomerService.Domains.Customers;
public class MaritalStatus :BaseEntity
{

    
    public required string Name { get; set; } 

    public virtual List<Customer>? Customers { get; set; } 
}
