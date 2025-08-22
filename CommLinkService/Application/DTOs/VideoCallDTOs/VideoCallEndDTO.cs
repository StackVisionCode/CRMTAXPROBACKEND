namespace DTOs.VideoCallDTOs;

public class VideoCallEndDTO
{
    public Guid CallId { get; set; }
    public DateTime EndedAt { get; set; }
    public bool Success { get; set; }
}
