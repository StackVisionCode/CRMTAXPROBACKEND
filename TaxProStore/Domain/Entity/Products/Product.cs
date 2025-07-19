using Application.Common;
using Application.Domain.Entity.Templates;

namespace Domain.Entity.Products;

public class Product : BaseEntity
{
    public Guid TemplateId { get; set; }
    public Guid SellerId { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; }
    public string Title { get; set; }
    //cantidad de ventas del producto
    public int SalesCount { get; set; } = 0;
    // Calificaciones del producto
    public double Rating { get; set; }
    //Me gusta y no me gusta del producto
    public int TotalRatings { get; set; } = 0;
    public int Likes { get; set; } = 0;
    public int Dislikes { get; set; } = 0;
    //contacto del vendedor
    public string SellerContact { get; set; } = string.Empty;
    // Descripción del producto
    public string Description { get; set; }
    public Template Templates { get; set; } = default!; // Relación con la plantilla del producto
    public Guid OwnerUserId { get; set; } // quien lo publica
    public ICollection<ProductFeedback> Feedbacks { get; set; }

}
