namespace CommLinkService.Domain.Entities;

public sealed class MessageReaction
{
    public Guid Id { get; private set; }
    public Guid MessageId { get; private set; }
    public Guid UserId { get; private set; }
    public string Emoji { get; private set; }
    public DateTime ReactedAt { get; private set; }

    private MessageReaction() { } // EF Core

    public MessageReaction(Guid messageId, Guid userId, string emoji)
    {
        Id = Guid.NewGuid();
        MessageId = messageId;
        UserId = userId;
        Emoji = emoji;
        ReactedAt = DateTime.UtcNow;
    }
}
