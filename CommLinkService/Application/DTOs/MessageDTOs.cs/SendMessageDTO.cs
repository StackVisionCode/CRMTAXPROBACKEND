using System.ComponentModel.DataAnnotations;

namespace DTOs.MessageDTOs;

public class SendMessageDTO
{
    public required Guid RoomId { get; set; }

    public ParticipantType SenderType { get; set; }
    public Guid? SenderTaxUserId { get; set; }
    public Guid? SenderCustomerId { get; set; }
    public Guid? SenderCompanyId { get; set; }

    [StringLength(4000)]
    public required string Content { get; set; } = string.Empty;

    public MessageType Type { get; set; } = MessageType.Text;
    public string? Metadata { get; set; }
}
