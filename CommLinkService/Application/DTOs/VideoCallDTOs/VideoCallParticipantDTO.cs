namespace DTOs.VideoCallDTOs;

public class VideoCallParticipantDTO
{
    public ParticipantType ParticipantType { get; set; }
    public Guid? TaxUserId { get; set; }
    public Guid? CustomerId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsMuted { get; set; }
    public bool IsVideoEnabled { get; set; }
}
