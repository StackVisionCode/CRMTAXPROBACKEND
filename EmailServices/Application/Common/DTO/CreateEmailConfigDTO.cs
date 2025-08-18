using System.ComponentModel.DataAnnotations;

namespace Application.Common.DTO;

public class CreateEmailConfigDTO
{
    public required Guid CompanyId { get; set; }
    public required Guid CreatedByTaxUserId { get; set; }

    [StringLength(120)]
    public required string Name { get; set; } = string.Empty;

    [StringLength(10)]
    public required string ProviderType { get; set; } = "Smtp";

    // SMTP fields
    public string? SmtpServer { get; set; }
    public int? SmtpPort { get; set; }
    public bool? EnableSsl { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }

    // Gmail fields
    public string? GmailClientId { get; set; }
    public string? GmailClientSecret { get; set; }
    public string? GmailRefreshToken { get; set; }
    public string? GmailEmailAddress { get; set; }

    [Range(1, 10000)]
    public int DailyLimit { get; set; } = 100;
}
