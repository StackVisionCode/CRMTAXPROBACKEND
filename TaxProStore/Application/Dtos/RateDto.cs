namespace Application.Dtos;
public class RateDto
{


    public Guid ProductId { get; set; }
    public Guid UserId { get; set; }
    public double Rating { get; set; } // de 1 a 5


}