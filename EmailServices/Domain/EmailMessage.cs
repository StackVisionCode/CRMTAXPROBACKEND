using Common;

namespace Domain;

public class EmailMessage:BaseEntity
{
     public required string To { get; set; } 
    public required string Subject { get; set; } 
    public required string Body { get; set; } 
    public required bool IsHtml { get; set; } 
    public string? Cc { get; set; }
    public string? Bcc { get; set; }
    public string? Attachments { get; set; } 
    public required bool Send { get; set; }
    
}