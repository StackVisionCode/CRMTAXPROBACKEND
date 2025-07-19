namespace Application.Dtos.Product;

public class LikeProductDto
{
    public Guid ProductId { get; set; }
    public Guid UserId { get; set; } // para evitar múltiples votos
}
