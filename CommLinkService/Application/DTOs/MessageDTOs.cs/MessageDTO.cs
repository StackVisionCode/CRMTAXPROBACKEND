namespace DTOs.MessageDTOs;

public class MessageDTO
{
    public Guid Id { get; set; }
    public Guid RoomId { get; set; }
    public ParticipantType SenderType { get; set; }
    public Guid? SenderTaxUserId { get; set; }
    public Guid? SenderCustomerId { get; set; }
    public Guid? SenderCompanyId { get; set; }
    public string SenderName { get; set; } = string.Empty; // Frontend resuelve
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? EditedAt { get; set; }
    public bool IsEdited => EditedAt.HasValue;
    public bool IsDeleted { get; set; }
    public List<MessageReactionDTO> Reactions { get; set; } = new();
}
