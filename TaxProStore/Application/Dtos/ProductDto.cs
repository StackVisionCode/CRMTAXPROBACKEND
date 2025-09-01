using System;

namespace Application.Dtos;

public class ProductDto
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public int Likes { get; set; }
    public double Rating { get; set; }
    public int TotalRatings { get; set; }
    public bool IsActive { get; set; }
}
