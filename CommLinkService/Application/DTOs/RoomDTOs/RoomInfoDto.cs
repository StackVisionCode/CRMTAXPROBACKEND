namespace DTOs.RoomDTOs;

public class RoomInfoDto
{
    public Guid RoomId { get; set; }
    public string Name { get; set; } = null!;
    public RoomType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime? LastActivityAt { get; set; }
}
