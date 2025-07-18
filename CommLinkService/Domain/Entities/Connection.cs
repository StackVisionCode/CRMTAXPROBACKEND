namespace CommLinkService.Domain.Entities;

public sealed class Connection
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string ConnectionId { get; private set; }
    public DateTime ConnectedAt { get; private set; }
    public DateTime? DisconnectedAt { get; private set; }
    public string? UserAgent { get; private set; }
    public string? IpAddress { get; private set; }
    public bool IsActive { get; private set; }

    private Connection() { } // EF Core

    public Connection(Guid userId, string connectionId, string? userAgent, string? ipAddress)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ConnectionId = connectionId;
        ConnectedAt = DateTime.UtcNow;
        UserAgent = userAgent;
        IpAddress = ipAddress;
        IsActive = true;
    }

    public void Disconnect()
    {
        DisconnectedAt = DateTime.UtcNow;
        IsActive = false;
    }
}
