using System.ComponentModel.DataAnnotations;

namespace Application.Common.DTO;

public class EmailTemplateDTO
{
    [Key]
    public Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid CreatedByTaxUserId { get; set; }
    public Guid? LastModifiedByTaxUserId { get; set; }

    [StringLength(120)]
    public required string Name { get; set; } = string.Empty;

    [StringLength(300)]
    public required string Subject { get; set; } = string.Empty;
    public required string BodyTemplate { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }

    [StringLength(2000)]
    public string TemplateVariables { get; set; } = string.Empty;
}
