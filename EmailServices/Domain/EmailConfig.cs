using Common;

namespace Domain;

public class EmailConfig
{
    public Guid Id { get; set; }
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
    public string? GmailAccessToken { get; set; } // store last access token (optional)
    public DateTime? GmailTokenExpiry { get; set; } // when the access token expires
    public string? GmailEmailAddress { get; set; } // Gmail account email (the user)
    public int DailyLimit { get; set; } = 100;
    public Guid UserId { get; set; }
}
