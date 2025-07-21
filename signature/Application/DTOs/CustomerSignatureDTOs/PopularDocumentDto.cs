namespace Application.DTOs.CustomerSignatureDTOs;

/// <summary>
/// DTO para documentos populares
/// </summary>
public class PopularDocumentDto
{
    public Guid DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public string Category { get; set; } = "Documento";
    public int SignatureCount { get; set; }
    public DateTime LastUsed { get; set; }
    public int TotalSigners { get; set; }
    public double AvgCompletionTimeHours { get; set; }
}
