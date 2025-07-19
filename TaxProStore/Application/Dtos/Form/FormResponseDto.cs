namespace Application.Dtos.Form;

public class FormResponseDto
{
   
    public Guid FormInstanceId { get; set; }
    public string Data { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}
