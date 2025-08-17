using System.ComponentModel.DataAnnotations;

namespace Application.Common.DTO;

public class EmailAttachmentDTO
{
    [Key]
    public Guid Id { get; set; }

    public required Guid EmailId { get; set; }

    public required Guid CompanyId { get; set; }

    [StringLength(255)]
    public required string FileName { get; set; } = string.Empty;

    [StringLength(100)]
    public required string ContentType { get; set; } = string.Empty;

    [Range(0, long.MaxValue)]
    public long Size { get; set; }

    public byte[]? Content { get; set; }

    [StringLength(500)]
    public string? FilePath { get; set; } // Para almacenamiento en disco

    public DateTime CreatedOn { get; set; }
}
