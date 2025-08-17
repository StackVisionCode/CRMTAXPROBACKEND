using Common;

namespace Domain;

public class EmailAttachment
{
    public Guid Id { get; set; }
    public Guid EmailId { get; set; }
    public required Guid CompanyId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public byte[]? Content { get; set; }
    public string? FilePath { get; set; } // Para almacenamiento en disco
    public DateTime CreatedOn { get; set; }
}
