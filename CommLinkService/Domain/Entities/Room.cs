using Common;

namespace CommLinkService.Domain.Entities;

public class Room : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public Guid CreatedByCompanyId { get; set; } // Company que crea el room
    public Guid CreatedByTaxUserId { get; set; } // TaxUser que lo creó
    public Guid? LastModifiedByTaxUserId { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool IsActive { get; set; } = true;
    public int MaxParticipants { get; set; } = 10;

    // Navegación
    public virtual ICollection<RoomParticipant> Participants { get; set; } =
        new List<RoomParticipant>();
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
