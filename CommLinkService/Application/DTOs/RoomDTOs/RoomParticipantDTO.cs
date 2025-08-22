namespace DTOs.RoomDTOs;

public class RoomParticipantDTO
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public ParticipantType ParticipantType { get; set; }
    public Guid? TaxUserId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? CompanyId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public ParticipantRole Role { get; set; }
    public bool IsOnline { get; set; }
    public bool IsMuted { get; set; }
    public bool IsVideoEnabled { get; set; }
    public DateTime JoinedAt { get; set; }
}
