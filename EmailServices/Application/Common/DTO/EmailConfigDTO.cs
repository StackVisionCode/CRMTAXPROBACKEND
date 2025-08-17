using System.ComponentModel.DataAnnotations;

namespace Application.Common.DTO;

public class EmailConfigDTO
{
    [Key]
    public Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid CreatedByTaxUserId { get; set; }
    public Guid? LastModifiedByTaxUserId { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    public string ProviderType { get; set; } = "Smtp"; // "Smtp" | "Gmail"

    // SMTP fields
    [StringLength(150)]
    public string? SmtpServer { get; set; }
    public int? SmtpPort { get; set; }
    public bool? EnableSsl { get; set; }

    [StringLength(150)]
    public string? SmtpUsername { get; set; }

    [StringLength(150)]
    public string? SmtpPassword { get; set; }

    // Gmail fields
    [StringLength(200)]
    public string? GmailClientId { get; set; }

    public string? GmailClientSecret { get; set; }
    public string? GmailRefreshToken { get; set; }
    public string? GmailAccessToken { get; set; }
    public DateTime? GmailTokenExpiry { get; set; }

    [StringLength(150)]
    public string? GmailEmailAddress { get; set; }

    [Range(1, 10000)]
    public int DailyLimit { get; set; } = 100;

    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}
