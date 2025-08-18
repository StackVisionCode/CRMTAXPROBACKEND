using Common;

namespace Domain;

public class EmailTemplate
{
    public Guid Id { get; set; }
    public required Guid CompanyId { get; set; }
    public required Guid CreatedByTaxUserId { get; set; }
    public Guid? LastModifiedByTaxUserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyTemplate { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string TemplateVariables { get; set; } = string.Empty; // JSON con variables disponibles
}
