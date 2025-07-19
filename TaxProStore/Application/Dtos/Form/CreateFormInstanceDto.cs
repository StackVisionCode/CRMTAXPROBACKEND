namespace Application.Dtos.Form;
public class CreateFormInstanceDto
{
    public Guid TemplateId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string CustomTitle { get; set; }
}