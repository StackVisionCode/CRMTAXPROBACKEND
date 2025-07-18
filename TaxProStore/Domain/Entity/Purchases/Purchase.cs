using Application.Common;
using Application.Domain.Entity.Products;

namespace Application.Domain.Entity.Purchases;

public class Purchase : BaseEntity
{

    public Guid BuyerId { get; set; }
    public Guid ProductId { get; set; }
   
    public string PaymentStatus { get; set; } = "Paid"; // o Pending, Failed
    public Product Products { get; set; } = default!; // Relación con el producto comprado
}
