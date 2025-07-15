namespace CommLinkService.Domain.Entities;

public sealed class Room
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public RoomType Type { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }
    public bool IsActive { get; private set; }
    public int MaxParticipants { get; private set; }

    private readonly List<RoomParticipant> _participants = new();
    public IReadOnlyCollection<RoomParticipant> Participants => _participants.AsReadOnly();

    private readonly List<Message> _messages = new();
    public IReadOnlyCollection<Message> Messages => _messages.AsReadOnly();

    private Room() { } // EF Core

    public Room(string name, RoomType type, Guid createdBy, int maxParticipants = 10)
    {
        Id = Guid.NewGuid();
        Name = name;
        Type = type;
        CreatedBy = createdBy;
        CreatedAt = DateTime.UtcNow;
        IsActive = true;
        MaxParticipants = maxParticipants;
        LastActivityAt = DateTime.UtcNow;
    }

    public void AddParticipant(Guid userId, ParticipantRole role)
    {
        if (_participants.Count >= MaxParticipants)
            throw new InvalidOperationException("Room is full");

        if (_participants.Any(p => p.UserId == userId))
            throw new InvalidOperationException("User already in room");

        _participants.Add(new RoomParticipant(Id, userId, role));
        LastActivityAt = DateTime.UtcNow;
    }

    public void RemoveParticipant(Guid userId)
    {
        _participants.RemoveAll(p => p.UserId == userId);
        LastActivityAt = DateTime.UtcNow;
    }

    public void AddMessage(Message message)
    {
        _messages.Add(message);
        LastActivityAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}

public enum RoomType
{
    Direct,
    Group,
    VideoCall,
}
