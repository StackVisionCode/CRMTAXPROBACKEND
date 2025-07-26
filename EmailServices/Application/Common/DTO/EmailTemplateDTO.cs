using System.ComponentModel.DataAnnotations;

namespace Application.Common.DTO;

public class EmailTemplateDTO
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(300)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string BodyTemplate { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid UserId { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime? UpdatedOn { get; set; }

    [StringLength(2000)]
    public string TemplateVariables { get; set; } = string.Empty;
}
