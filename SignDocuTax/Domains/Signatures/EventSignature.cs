using Common;
using Domains.Requirements;
using Domains.Signatures;

namespace Domain.Signatures;

public class EventSignature : BaseEntity
{
  // Relaciones clave
  public int RequirementSignatureId { get; set; }
  public int AnswerRequirementId { get; set; } // Nueva relación
  public int DocumentId { get; set; } // Referencia directa al documento

  // Información del firmante
  public int TaxUserId { get; set; }
  public int CompanyId { get; set; }

  // Metadatos técnicos
  public string IpAddress { get; set; } = string.Empty;
  public required string DeviceName { get; set; }
  public string? DeviceOs { get; set; } // Nuevo: Sistema operativo
  public string? Browser { get; set; } // Nuevo: Navegador usado

  // Datos de la firma
  public DateTime SignatureDate { get; set; } = DateTime.UtcNow;
  public string? DigitalSignatureHash { get; set; } // Hash criptográfico
  public string? SignatureImageUrl { get; set; } // URL de la imagen de firma

  // Auditoría
  public string? AuditTrailJson { get; set; } // Datos completos en JSON
  public bool IsValid { get; set; } = true; // Para invalidar firmas si es necesario

  // Nuevo: Tipo de evento (firma, rechazo, visualización)
  public int SignatureEventTypeId { get; set; }

  public string? TimestampToken { get; set; } // Sello de tiempo RFC 3161  
  public string? TimestampAuthority { get; set; } // Ej: "DigiCert"  
  public string? SignatureLevel { get; set; } // "Simple/Advanced/Digital"  
  public string? DocumentHashAtSigning { get; set; } // SHA-256 del PDF al firmar

  public SignatureEventType SignatureEventType { get; set; } = null!;
  public AnswerRequirement AnswerRequirement { get; set; } = null!;
  public RequirementSignature RequirementSignature { get; set; } = null!;
}