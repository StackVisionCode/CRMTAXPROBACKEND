using Common;

namespace Domain;

public class EmailConfig
{
    public Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid CreatedByTaxUserId { get; set; }
    public Guid? LastModifiedByTaxUserId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? ProviderType { get; set; } // "Smtp" or "Gmail"

    // SMTP configuration fields
    public string? SmtpServer { get; set; }
    public int? SmtpPort { get; set; }
    public bool? EnableSsl { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }

    // Gmail API configuration fields
    public string? GmailClientId { get; set; }
    public string? GmailClientSecret { get; set; }
    public string? GmailRefreshToken { get; set; }
    public string? GmailAccessToken { get; set; }
    public DateTime? GmailTokenExpiry { get; set; }
    public string? GmailEmailAddress { get; set; }

    public int DailyLimit { get; set; } = 100;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}
