namespace Application.Dtos.Form;


public class FormInstanceDto
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
   
    public Guid OwnerUserId { get; set; } // Quien usará la plantilla

    public string CustomTitle { get; set; }
    public TemplateDto Template { get; set; }

   public ICollection<FormResponseDto> Responses { get; set; } = new List<FormResponseDto>();
}
