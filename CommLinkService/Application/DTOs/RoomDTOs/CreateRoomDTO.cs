using System.ComponentModel.DataAnnotations;

namespace DTOs.RoomDTOs;

public class CreateRoomDTO
{
    [StringLength(100)]
    public required string Name { get; set; } = string.Empty;

    public RoomType Type { get; set; }

    public required Guid CreatedByCompanyId { get; set; }
    public required Guid CreatedByTaxUserId { get; set; }

    [Range(1, 100)]
    public int MaxParticipants { get; set; } = 10;

    public Guid? CustomerId { get; set; }
}
