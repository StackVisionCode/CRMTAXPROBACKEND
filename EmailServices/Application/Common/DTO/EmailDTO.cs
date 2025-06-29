using System.ComponentModel.DataAnnotations;

namespace Application.Common.DTO;

public class EmailDTO
{
    [Key]
    public Guid Id { get; set; }
    public Guid ConfigId { get; set; }
    public string? FromAddress { get; set; }
    public string ToAddresses { get; set; } = string.Empty;
    public string? CcAddresses { get; set; }
    public string? BccAddresses { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime? SentOn { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid UserId { get; set; }
}
