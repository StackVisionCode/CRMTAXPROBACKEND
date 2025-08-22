using System.ComponentModel.DataAnnotations;

namespace DTOs.ConnectionDTOs;

public class CreateConnectionDTO
{
    public ParticipantType UserType { get; set; }

    public Guid? TaxUserId { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid? CompanyId { get; set; }

    [StringLength(256)]
    public required string ConnectionId { get; set; } = string.Empty;

    [StringLength(512)]
    public string? UserAgent { get; set; }

    [StringLength(64)]
    public string? IpAddress { get; set; }
}
