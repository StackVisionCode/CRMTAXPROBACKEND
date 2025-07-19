namespace Application.Dtos;
public class CreateProductDto
{
    public Guid TemplateId { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public Guid OwnerUserId { get; set; }
}