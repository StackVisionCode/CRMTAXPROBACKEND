namespace BankStaments.Domain.Tbl_IA_Entity;

public class DeepSeekApiRequest
{
    public string DocumentUrl { get; set; }
    public string DocumentType { get; set; }
    public string ProcessingType { get; set; } = "bank_statement";
    public Dictionary<string, string> Metadata { get; set; }
}
