namespace Application.DTOs;

public class RegisterConsentDto
{
    public required string Token { get; set; }
    public DateTime AgreedAtUtc { get; set; } = DateTime.UtcNow;
    public string? ConsentText { get; set; }
    public bool? ButtonText { get; set; }
    public required string UserAgent { get; set; }
    public required string ClientIp { get; set; }
}
