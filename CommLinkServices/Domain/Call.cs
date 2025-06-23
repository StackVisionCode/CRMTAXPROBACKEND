using Common;

namespace CommLinkServices.Domain;

public class Call : BaseEntity
{
    public required Guid ConversationId { get; set; }
    public required Guid StarterId { get; set; }
    public required CallType Type { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
