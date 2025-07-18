using System;
namespace Application.Dtos;
public class ProductDto
{
    public Guid TemplateId { get; set; }
    public Guid SellerId { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "USD";
    public bool IsActive { get; set; }
    public required string Title { get; set; }
    public  string? Description { get; set; }
   
}