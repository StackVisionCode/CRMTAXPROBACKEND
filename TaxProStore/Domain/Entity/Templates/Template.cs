using Application.Common;
using Domain.Entity.Form;

namespace Application.Domain.Entity.Templates;

public class Template : BaseEntity
{
    public Guid OwnerUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string HtmlContent { get; set; } = string.Empty;
    public string? PreviewUrl { get; set; }
    public bool IsPublished { get; set; }
    public bool IsPublic { get; set; } = false;

     public ICollection<FormInstance>? FormInstances { get; set; }
   
}
