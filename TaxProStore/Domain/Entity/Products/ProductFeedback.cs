
namespace Domain.Entity.Products;
public class ProductFeedback
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
     public Product Product { get; set; }
    public Guid UserId { get; set; }
    public bool? IsLike { get; set; } // null = no votó, true = like, false = dislike
    public double? Rating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
    