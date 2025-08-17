using System.ComponentModel.DataAnnotations;

namespace Application.Common.DTO;

public class IncomingEmailDTO
{
    [Key]
    public Guid Id { get; set; }
    public required Guid ConfigId { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid CreatedByTaxUserId { get; set; }

    [StringLength(200)]
    public required string FromAddress { get; set; } = string.Empty;

    [StringLength(200)]
    public required string ToAddress { get; set; } = string.Empty;

    [StringLength(500)]
    public string? CcAddresses { get; set; }

    [StringLength(300)]
    public required string Subject { get; set; } = string.Empty;

    public required string Body { get; set; } = string.Empty;

    public DateTime ReceivedOn { get; set; }
    public bool IsRead { get; set; }

    [StringLength(200)]
    public string? MessageId { get; set; }

    public List<EmailAttachmentDTO> Attachments { get; set; } = new();
}
