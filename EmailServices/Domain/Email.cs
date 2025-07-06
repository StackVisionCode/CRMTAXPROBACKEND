using Common;
using EmailServices.Domain;

namespace Domain;

public class Email
{
    public Guid Id { get; set; }
    public Guid ConfigId { get; set; }
    public string? FromAddress { get; set; }
    public string? ToAddresses { get; set; }
    public string? CcAddresses { get; set; }
    public string? BccAddresses { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public EmailStatus Status { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? SentOn { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid SentByUserId { get; set; }
}
