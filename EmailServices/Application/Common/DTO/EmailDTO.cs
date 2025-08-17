using System.ComponentModel.DataAnnotations;

namespace Application.Common.DTO;

public class EmailDTO
{
    [Key]
    public Guid Id { get; set; }
    public required Guid ConfigId { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid CreatedByTaxUserId { get; set; }
    public Guid? LastModifiedByTaxUserId { get; set; }
    public required Guid SentByTaxUserId { get; set; }
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

    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public DateTime? SentOn { get; set; }
    public string? Status { get; set; }
    public string? ErrorMessage { get; set; }
}
