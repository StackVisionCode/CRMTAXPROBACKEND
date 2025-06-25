using Common;

namespace CommLinkServices.Domain;

public class Message : BaseEntity
{
    public required Guid ConversationId { get; set; }
    public required Guid SenderId { get; set; }
    public required string Content { get; set; }
    public bool HasAttachment { get; set; }
    public string? AttachmentUrl { get; set; }
    public DateTime SentAt { get; set; }
}
