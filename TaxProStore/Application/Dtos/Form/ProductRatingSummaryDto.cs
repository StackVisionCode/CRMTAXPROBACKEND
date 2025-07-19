namespace Application.Dtos.Product;

public class ProductRatingSummaryDto
{
    public Guid ProductId { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int Likes { get; set; }
    public int Dislikes { get; set; }
}
