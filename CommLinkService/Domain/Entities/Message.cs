using Common;

namespace CommLinkService.Domain.Entities;

public class Message : BaseEntity
{
    public Guid RoomId { get; set; }
    public ParticipantType SenderType { get; set; }
    public Guid? SenderTaxUserId { get; set; } // Si es staff
    public Guid? SenderCustomerId { get; set; } // Si es cliente
    public Guid? SenderCompanyId { get; set; } // Company del TaxUser (null si es Customer)
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    public string? Metadata { get; set; }

    // Navigation
    public virtual Room Room { get; set; } = null!;
    public virtual ICollection<MessageReaction> Reactions { get; set; } =
        new List<MessageReaction>();
}
