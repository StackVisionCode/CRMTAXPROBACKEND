using System;
namespace Application.Dtos;

public class TemplateDto
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string? PreviewUrl { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
