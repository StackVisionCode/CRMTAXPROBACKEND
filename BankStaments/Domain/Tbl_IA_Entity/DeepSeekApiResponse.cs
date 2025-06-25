namespace BankStaments.Domain.Tbl_IA_Entity;
public class DeepSeekApiResponse
{
    public string? RequestId { get; set; }
    public string? Status { get; set; }
    public ProcessedDocumentData? Data { get; set; }
    public DateTime ProcessedAt { get; set; }
}