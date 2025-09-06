namespace CommLinkService.Application.DTOs.VideoCallDTOs;

public class TurnCredentialsDto
{
    public string Username { get; set; } = string.Empty;
    public string Credential { get; set; } = string.Empty;
    public string[] Urls { get; set; } = Array.Empty<string>();
    public int TtlSeconds { get; set; }
    public DateTime ExpiresAt { get; set; }
}

public class TurnServerHealthDto
{
    public bool IsHealthy { get; set; }
    public string ServerUrl { get; set; } = string.Empty;
    public int ResponseTimeMs { get; set; }
    public DateTime CheckedAt { get; set; }
}
