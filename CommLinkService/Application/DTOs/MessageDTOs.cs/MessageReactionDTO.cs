namespace DTOs.MessageDTOs;

public class MessageReactionDTO
{
    public Guid Id { get; set; }
    public Guid MessageId { get; set; }
    public ParticipantType ReactorType { get; set; }
    public Guid? ReactorTaxUserId { get; set; }
    public Guid? ReactorCustomerId { get; set; }
    public Guid? ReactorCompanyId { get; set; }
    public string ReactorName { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public DateTime ReactedAt { get; set; }
}
