namespace BankStaments.Domain.Tbl_IA_Entity;
public class DocumentProcessingRequest
{
    public IFormFile File { get; set; }
    public string AccountNumber { get; set; }
    public string CustomerId { get; set; }
    public string DocumentType { get; set; } // "statement", "invoice", "receipt", etc.
}