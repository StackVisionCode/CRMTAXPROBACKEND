using Common;

namespace CommLinkService.Domain.Entities;

public sealed class Connection : BaseEntity
{
    public ParticipantType UserType { get; set; }
    public Guid? TaxUserId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CompanyId { get; set; }

    public string ConnectionId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DisconnectedAt { get; set; }
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public bool IsActive { get; set; } = true;
}
