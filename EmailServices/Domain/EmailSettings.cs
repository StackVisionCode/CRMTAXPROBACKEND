using Common;

namespace Domain;

public class EmailSettings :BaseEntity
{
    public required int CompanyId { get; set; }
    public required int UserId { get; set; }
    public required string SmtpServer { get; set; }
    public required int Port { get; set; }
    public required string  SenderEmail { get; set; }
    public required string SenderName { get; set; }
    public required string  Username { get; set; }
    public required string Password { get; set; }
    public bool IsDefault { get; set; }
  
}