using Application.Common;

namespace Application.Domain.Entity.Templates;

public class Template: BaseEntity
{
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; }= string.Empty;
    public string HtmlContent { get; set; }= string.Empty;
    public string? PreviewUrl { get; set; }
    public bool IsPublished { get; set; }
}
