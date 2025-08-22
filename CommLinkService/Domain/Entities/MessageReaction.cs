using Common;

namespace CommLinkService.Domain.Entities;

public class MessageReaction : BaseEntity
{
    public Guid MessageId { get; set; }
    public ParticipantType ReactorType { get; set; }
    public Guid? ReactorTaxUserId { get; set; }
    public Guid? ReactorCustomerId { get; set; }
    public Guid? ReactorCompanyId { get; set; }

    public string Emoji { get; set; } = string.Empty;
    public DateTime ReactedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual Message Message { get; set; } = null!;
}
