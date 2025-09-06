namespace CommLinkService.Infrastructure.Configuration;

public class TurnServerConfig
{
    public string ServerDomain { get; set; } = string.Empty;
    public int TurnPort { get; set; } = 3478;
    public int TurnsPort { get; set; } = 5349;
    public string SharedSecret { get; set; } = string.Empty;
    public int CredentialTtlSeconds { get; set; } = 3600; // 1 hora
    public bool EnableTurns { get; set; } = true;
}
