using System.ComponentModel.DataAnnotations;

namespace Application.Common.DTO;

public class UpdateEmailDTO
{
    public required Guid CompanyId { get; set; }
    public required Guid LastModifiedByTaxUserId { get; set; }

    public required Guid ConfigId { get; set; }

    public string? FromAddress { get; set; }

    [Required]
    [StringLength(500)]
    public string ToAddresses { get; set; } = string.Empty;

    [StringLength(500)]
    public string? CcAddresses { get; set; }

    [StringLength(500)]
    public string? BccAddresses { get; set; }

    [Required]
    [StringLength(300)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;
}
