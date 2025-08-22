namespace DTOs.VideoCallDTOs;

public class VideoCallDTO
{
    public Guid CallId { get; set; }
    public Guid RoomId { get; set; }
    public string RoomName { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public string SignalServer { get; set; } = string.Empty;
    public Dictionary<string, object> IceServers { get; set; } = new();
    public List<VideoCallParticipantDTO> Participants { get; set; } = new();
}
