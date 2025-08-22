using Common;

namespace CommLinkService.Domain.Entities;

public class RoomParticipant : BaseEntity
{
    public Guid RoomId { get; set; }
    public ParticipantType ParticipantType { get; set; }
    public Guid? TaxUserId { get; set; } // Para staff/employees
    public Guid? CustomerId { get; set; } // Para clientes
    public Guid? CompanyId { get; set; } // Company del TaxUser (null si es Customer)
    public Guid AddedByCompanyId { get; set; }
    public Guid AddedByTaxUserId { get; set; }

    public ParticipantRole Role { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public bool IsMuted { get; set; } = false;
    public bool IsVideoEnabled { get; set; } = false;

    // Navigation
    public virtual Room Room { get; set; } = null!;
}
