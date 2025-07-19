using Application.Common;
using Application.Domain.Entity.Templates;
using Domain.Entity.Form;

public class FormInstance: BaseEntity
{
    public Guid TemplateId { get; set; }
    public Template Template { get; set; } 

    public Guid OwnerUserId { get; set; } // Quien usará la plantilla

    public string CustomTitle { get; set; }

    public ICollection<FormResponse> Responses { get; set; }= new List<FormResponse>();
}
