using System.ComponentModel.DataAnnotations;
namespace SMSServices.Application.DTO;


public class SmsRequestDto
{
    [Required]
    [Phone]
    public string To { get; set; } = string.Empty;
    
    [Required]
    [StringLength(1600, MinimumLength = 1)]
    public string Message { get; set; } = string.Empty;
    
    // // Opcional - para usar plantillas
    // public string? TemplateName { get; set; }
    // public Dictionary<string, string>? TemplateParams { get; set; }
}