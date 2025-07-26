using System.ComponentModel.DataAnnotations;

namespace Application.Common.DTO;

public class IncomingEmailDTO
{
    [Key]
    public Guid Id { get; set; }
    public Guid ConfigId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string? CcAddresses { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime ReceivedOn { get; set; }
    public bool IsRead { get; set; }
    public string? MessageId { get; set; }
    public List<EmailAttachmentDTO> Attachments { get; set; } = new();
}
