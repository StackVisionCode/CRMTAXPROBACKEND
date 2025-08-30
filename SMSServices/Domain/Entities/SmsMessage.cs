using System.ComponentModel.DataAnnotations;

namespace SMSServices.Domain.Entities;

public class SmsMessage
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(50)]
    public string MessageSid { get; set; } = string.Empty; // ID de Twilio
    
    [Required]
    [StringLength(20)]
    public string From { get; set; } = string.Empty; // +1234567890
    
    [Required]
    [StringLength(20)]
    public string To { get; set; } = string.Empty; // +1987654321
    
    [Required]
    [StringLength(1600)] // SMS puede tener hasta 1600 caracteres
    public string Body { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string Status { get; set; } = string.Empty; // sent, delivered, failed, etc.
    
    [Required]
    public SmsDirection Direction { get; set; } // Outbound o Inbound
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Información adicional de Twilio
    public decimal? Price { get; set; }
    [StringLength(10)]
    public string? PriceUnit { get; set; }
    
    [StringLength(10)]
    public string? NumSegments { get; set; } // Número de segmentos del SMS
    
    [StringLength(10)]
    public string? NumMedia { get; set; } // Para MMS
    
    [StringLength(200)]
    public string? ErrorCode { get; set; }
    
    [StringLength(500)]
    public string? ErrorMessage { get; set; }
    
    // Para mensajes entrantes
    [StringLength(50)]
    public string? AccountSid { get; set; }
    
    [StringLength(50)]
    public string? MessagingServiceSid { get; set; }
}
