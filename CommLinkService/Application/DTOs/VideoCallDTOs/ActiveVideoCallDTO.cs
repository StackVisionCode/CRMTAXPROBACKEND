namespace DTOs.VideoCallDTOs;

public class ActiveVideoCallDTO
{
    public Guid CallId { get; set; }
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public List<Guid> ParticipantIds { get; set; } = new();
}
