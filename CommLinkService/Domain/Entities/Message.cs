namespace CommLinkService.Domain.Entities;

public sealed class Message
{
    public Guid Id { get; private set; }
    public Guid RoomId { get; private set; }
    public Guid SenderId { get; private set; }
    public string Content { get; private set; }
    public MessageType Type { get; private set; }
    public DateTime SentAt { get; private set; }
    public DateTime? EditedAt { get; private set; }
    public bool IsDeleted { get; private set; }
    public string? Metadata { get; private set; } // JSON for attachments, etc.

    private readonly List<MessageReaction> _reactions = new();
    public IReadOnlyCollection<MessageReaction> Reactions => _reactions.AsReadOnly();

    private Message() { } // EF Core

    public Message(
        Guid roomId,
        Guid senderId,
        string content,
        MessageType type,
        string? metadata = null
    )
    {
        Id = Guid.NewGuid();
        RoomId = roomId;
        SenderId = senderId;
        Content = content;
        Type = type;
        SentAt = DateTime.UtcNow;
        IsDeleted = false;
        Metadata = metadata;
    }

    public void Edit(string newContent)
    {
        Content = newContent;
        EditedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        IsDeleted = true;
        Content = "[Message deleted]";
    }

    public void AddReaction(Guid userId, string emoji)
    {
        if (_reactions.Any(r => r.UserId == userId && r.Emoji == emoji))
            return;

        _reactions.Add(new MessageReaction(Id, userId, emoji));
    }
}

public enum MessageType
{
    Text,
    Image,
    Video,
    File,
    System,
    VideoCallStart,
    VideoCallEnd,
}
