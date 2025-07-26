using Common;
using EmailServices.Domain;

namespace Domain;

public class IncomingEmail
{
    public Guid Id { get; set; }
    public Guid ConfigId { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string ToAddress { get; set; } = string.Empty;
    public string? CcAddresses { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime ReceivedOn { get; set; }
    public bool IsRead { get; set; }
    public string? MessageId { get; set; } // ID único del mensaje del servidor
    public string? InReplyTo { get; set; } // Para threading de conversaciones
    public string? References { get; set; } // Para threading de conversaciones
    public List<EmailAttachment> Attachments { get; set; } = new();
    public Guid UserId { get; set; } // Dueño de la configuración
}
