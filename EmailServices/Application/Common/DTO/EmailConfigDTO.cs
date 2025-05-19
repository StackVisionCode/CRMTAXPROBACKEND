using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Application.Common.DTO;
public class EmailConfigDTO
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProviderType { get; set; } = "Smtp";   // “Smtp” | “Gmail”
    public string? SmtpServer { get; set; }
    public int?    SmtpPort   { get; set; }
    public bool?   EnableSsl  { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public string? GmailClientId     { get; set; }
    public string? GmailClientSecret { get; set; }
    public string? GmailRefreshToken { get; set; }
    public string? GmailAccessToken  { get; set; }
    public DateTime? GmailTokenExpiry { get; set; }
    public string? GmailEmailAddress  { get; set; }
    public int  DailyLimit { get; set; } = 100;
    public int CompanyId { get; set; }
    public int UserId { get; set; }
}
