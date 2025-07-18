namespace Application.Dtos;
public class CreateDto
{
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string? PreviewUrl { get; set; }
    public bool IsPublished { get; set; }

}
