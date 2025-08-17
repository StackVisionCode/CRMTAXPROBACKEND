namespace Application.Dtos.Form;

public class FormResponseResult
{
    public Guid ResponseId { get; set; }
    public Guid FormInstanceId { get; set; }
    public int FieldsCount { get; set; }
    public int FilesCount { get; set; }
    public List<string> ProcessedFiles { get; set; } = new();
    public DateTime SubmittedAt { get; set; }
    public bool Success { get; set; }
}
