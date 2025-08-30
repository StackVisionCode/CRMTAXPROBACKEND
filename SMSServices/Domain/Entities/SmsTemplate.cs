using System.ComponentModel.DataAnnotations;
namespace SMSServices.Domain.Entities;

public class SmsTemplate
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1600)]
    public string Template { get; set; } = string.Empty; // "Hola {nombre}, tu código es {codigo}"
    
    [StringLength(500)]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    [StringLength(50)]
    public string CreatedBy { get; set; } = string.Empty;
}
