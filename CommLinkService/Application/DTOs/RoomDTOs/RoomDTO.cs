using DTOs.MessageDTOs;

namespace DTOs.RoomDTOs;

public class RoomDTO
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public RoomType Type { get; set; }
    public Guid CreatedByCompanyId { get; set; }
    public Guid CreatedByTaxUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public bool IsActive { get; set; }
    public int MaxParticipants { get; set; }
    public int ParticipantCount { get; set; }
    public List<RoomParticipantDTO> Participants { get; set; } = new();
    public MessageDTO? LastMessage { get; set; }
    public int UnreadCount { get; set; }
}
