using System.ComponentModel.DataAnnotations;

namespace Application.Common.DTO;

public class EmailAttachmentDTO
{
    [Key]
    public Guid Id { get; set; }
    public Guid EmailId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long Size { get; set; }
    public byte[]? Content { get; set; }
    public string? FilePath { get; set; } // Para almacenamiento en disco
}
