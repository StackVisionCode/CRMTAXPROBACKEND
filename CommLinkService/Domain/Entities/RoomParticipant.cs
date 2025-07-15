namespace CommLinkService.Domain.Entities;

public sealed class RoomParticipant
{
    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public Room Room { get; private set; } = null!; // Navigation property
    public Guid UserId { get; private set; }
    public ParticipantRole Role { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsMuted { get; private set; }
    public bool IsVideoEnabled { get; private set; }

    private RoomParticipant() { } // EF Core

    public RoomParticipant(Guid roomId, Guid userId, ParticipantRole role)
    {
        Id = Guid.NewGuid();
        RoomId = roomId;
        UserId = userId;
        Role = role;
        JoinedAt = DateTime.UtcNow;
        IsActive = true;
        IsMuted = false;
        IsVideoEnabled = false;
    }

    public void SetMuted(bool muted) => IsMuted = muted;

    public void SetVideoEnabled(bool enabled) => IsVideoEnabled = enabled;

    public void SetInactive() => IsActive = false;

    public void SetActive() => IsActive = true;
}

public enum ParticipantRole
{
    Member,
    Admin,
    Owner,
}
