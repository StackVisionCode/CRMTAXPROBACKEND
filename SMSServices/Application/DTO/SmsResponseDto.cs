using SMSServices.Domain.Entities;

namespace SMSServices.Application.DTO;
public class SmsResponseDto
{
    public Guid Id { get; set; }
    public string MessageSid { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public required string From { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public SmsDirection Direction { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal? Price { get; set; }
    public string? PriceUnit { get; set; }
    public string? NumSegments { get; set; }
    public string? ErrorMessage { get; set; }
}