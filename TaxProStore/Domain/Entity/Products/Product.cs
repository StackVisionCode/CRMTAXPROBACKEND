using Application.Common;
using Application.Domain.Entity.Templates;

namespace Application.Domain.Entity.Products;

public class Product : BaseEntity
{

    public Guid TemplateId { get; set; }
    public Guid SellerId { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public Template Templates { get; set; } = default!; // Relación con la plantilla del producto
}
